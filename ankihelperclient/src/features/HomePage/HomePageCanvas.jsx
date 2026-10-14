import { useEffect, useMemo, useRef, useState } from "react"
import { useNavigate } from "react-router-dom";
import Canvas from "../../Components/Canvas/Canvas";
import ankiHelperApi from "../../api/ankiHelperApi"
import Title from "../../Components/Title/Title";
import styles from "./HomePageCanvas.module.css";
import { useLanguage } from "../../i18n/useLanguage";

const HomePageCanvas = () => {
    const [isCheckingConnectionSynchronization, setIsCheckingConnectionSynchronization] = useState(false)
    const [isCheckingConnectionAddTask, setIsCheckingConnectionAddTask] = useState(false)
    const [isDeletingDecks, setIsDeletingDecks] = useState(false)
    const [isDeleteMode, setIsDeleteMode] = useState(false)
    const [isConnected, setIsConnected] = useState(false)
    const [synchronizationError, setSynchronizationError] = useState("")
    const [deleteError, setDeleteError] = useState("")
    const [selectedDeckIds, setSelectedDeckIds] = useState([])
    const [activeDeckMenu, setActiveDeckMenu] = useState(null)
    const [deckStats, setDeckStats] = useState([])
    const [ankiSource, setAnkiSource] = useState("")
    const [hasLoadedInitialData, setHasLoadedInitialData] = useState(false)
    const { t } = useLanguage()
    const apkgSynchronizationInputRef = useRef(null)
    const deckMenuRef = useRef(null)
    const navigate = useNavigate();
    const isHomeBusy = isCheckingConnectionAddTask || isCheckingConnectionSynchronization || isDeletingDecks
    const isApkgSource = ankiSource === "Apkg"
    const hasDecks = deckStats.length > 0
    const canSynchronize = ankiSource !== ""
    const totalTodayCount = useMemo(
        () => deckStats.reduce((total, deck) => total + deck.weakNotes + deck.newNotes + deck.reviewNotes, 0),
        [deckStats]
    )

    const sortDecksById = (decks) => {
        return [...decks].sort((firstDeck, secondDeck) => firstDeck.deckId - secondDeck.deckId)
    }

    const loadDeckStats = async () => {
        const result = await ankiHelperApi.getDecksStats()
        setDeckStats(sortDecksById(result))
    }

    useEffect(() => {
        let shouldIgnoreResult = false

        const loadInitialDeckStats = async () => {
            try {
                const [result, source] = await Promise.all([
                    ankiHelperApi.getDecksStats(),
                    ankiHelperApi.getAnkiSource()
                ])

                if (!shouldIgnoreResult) {
                    setDeckStats(sortDecksById(result))
                    setAnkiSource(source)
                }
            } finally {
                if (!shouldIgnoreResult) {
                    setHasLoadedInitialData(true)
                }
            }
        }

        loadInitialDeckStats()

        return () => {
            shouldIgnoreResult = true
        }
    }, [])

    useEffect(() => {
        if (activeDeckMenu === null) {
            return
        }

        const closeDeckMenuOnOutsideClick = ({ target }) => {
            if (!deckMenuRef.current?.contains(target)) {
                setActiveDeckMenu(null)
            }
        }

        const closeDeckMenuOnEscape = ({ key }) => {
            if (key === "Escape") {
                setActiveDeckMenu(null)
            }
        }

        document.addEventListener("pointerdown", closeDeckMenuOnOutsideClick)
        document.addEventListener("keydown", closeDeckMenuOnEscape)

        return () => {
            document.removeEventListener("pointerdown", closeDeckMenuOnOutsideClick)
            document.removeEventListener("keydown", closeDeckMenuOnEscape)
        }
    }, [activeDeckMenu])

    const handleAddDeck = async () => {
        setIsCheckingConnectionAddTask(true)
        setDeleteError("")

        if (isApkgSource) {
            navigate("/add-deck", {
                state: {
                    ankiSource,
                    existingDeckIds: deckStats.map(deck => deck.deckId)
                }
            })
            setIsCheckingConnectionAddTask(false)
            return
        }

        const isConnected = await ankiHelperApi.checkConnection()

        if (isConnected) {
            navigate("/add-deck", {
                state: {
                    ankiSource,
                    existingDeckIds: deckStats.map(deck => deck.deckId)
                }
            })
        } else {
            alert(t("common.openAnki"))
        }

        setIsCheckingConnectionAddTask(false)
    }

    const handleSynchronization = async () => {
        if (!hasDecks) {
            return
        }

        if (isApkgSource) {
            apkgSynchronizationInputRef.current?.click()
            return
        }

        setIsCheckingConnectionSynchronization(true)
        setIsConnected(false)
        setSynchronizationError("")
        setDeleteError("")

        try {
            const isConnected = await ankiHelperApi.checkConnection()

            if (isConnected) {
                setIsConnected(true)

                await ankiHelperApi.postSynchronization()
                await loadDeckStats()
            } else {
                alert(t("common.openAnki"))
            }

        } catch (error) {
            setSynchronizationError(error.message || t("home.syncFailed"))
        } finally {
            setIsConnected(false)
            setIsCheckingConnectionSynchronization(false)
        }
    }

    const handleApkgSynchronizationFileChange = async ({ target }) => {
        const file = target.files?.[0]

        if (!file) {
            return
        }

        setIsCheckingConnectionSynchronization(true)
        setIsConnected(true)
        setSynchronizationError("")
        setDeleteError("")

        try {
            await ankiHelperApi.uploadAnkiApkgFile(file)
            await ankiHelperApi.postSynchronization()
            await loadDeckStats()
        } catch (error) {
            setSynchronizationError(error.message || t("home.syncFailed"))
        } finally {
            setIsConnected(false)
            setIsCheckingConnectionSynchronization(false)
            target.value = ""
        }
    }

    const handleDeckStatsClick = (deck) => {
        if (isDeleteMode) {
            toggleDeckSelection(deck.deckId)
            return
        }

        const id = deck.deckId
        navigate("select-training", { state: { id } })
    }

    const handleDeckContextMenu = (event, deckId) => {
        if (isHomeBusy || isDeleteMode) {
            return
        }

        event.preventDefault()

        const menuWidth = 150
        const menuHeight = 44
        const viewportPadding = 12

        setActiveDeckMenu({
            deckId,
            x: Math.min(event.clientX, window.innerWidth - menuWidth - viewportPadding),
            y: Math.min(event.clientY, window.innerHeight - menuHeight - viewportPadding)
        })
    }

    const toggleDeleteMode = () => {
        if (isHomeBusy) {
            return
        }

        setIsDeleteMode(current => !current)
        setSelectedDeckIds([])
        setDeleteError("")
    }

    const toggleDeckSelection = (deckId) => {
        setSelectedDeckIds(current =>
            current.includes(deckId)
                ? current.filter(id => id !== deckId)
                : [...current, deckId]
        )
    }

    const deleteDecks = async (deckIds) => {
        if (deckIds.length === 0) {
            return
        }

        const confirmed = window.confirm(t("home.deleteConfirm", {
            count: deckIds.length,
            plural: deckIds.length === 1 ? "" : "s"
        }))

        if (!confirmed) {
            return
        }

        setIsDeletingDecks(true)
        setDeleteError("")

        try {
            await ankiHelperApi.deleteDecks(deckIds)
            await loadDeckStats()
            setSelectedDeckIds([])
            setIsDeleteMode(false)
            setActiveDeckMenu(null)
        } catch (error) {
            setDeleteError(error.message || t("home.deleteFailed"))
        } finally {
            setIsDeletingDecks(false)
        }
    }

    const handleDeleteDecks = async () => {
        await deleteDecks(selectedDeckIds)
    }

    const handleDeleteSingleDeck = async (deckId) => {
        await deleteDecks([deckId])
    }

    const getTodayCount = (deck) => {
        return deck.weakNotes + deck.newNotes + deck.reviewNotes
    }

    return (
        <div>
            <Canvas>
                <div className={styles.header}>
                    <div className={styles.titleGroup}>
                        <Title>{t("home.title")}</Title>
                        <p className={styles.totalToday}>
                            {t("home.today")} <strong>{totalTodayCount}</strong>
                        </p>
                    </div>

                    <div className={styles.headerActions}>
                        <input
                            className={styles.hiddenFileInput}
                            ref={apkgSynchronizationInputRef}
                            type="file"
                            accept=".apkg"
                            onChange={handleApkgSynchronizationFileChange}
                        />

                        <button
                            className={styles.iconButton}
                            type="button"
                            onClick={handleAddDeck}
                            disabled={isHomeBusy || isDeleteMode}
                            aria-label={t("home.addNewDeck")}
                            title={t("home.addNewDeck")}
                        >
                            {isCheckingConnectionAddTask ? (
                                <span className={styles.buttonSpinner}></span>
                            ) : (
                                "+"
                            )}
                        </button>

                        {canSynchronize && (
                            <button
                                className={styles.iconButton}
                                type="button"
                                onClick={handleSynchronization}
                                disabled={isHomeBusy || isDeleteMode || !hasDecks}
                                aria-label={t("home.synchronization")}
                                title={t("home.synchronization")}
                            >
                                <span className={isCheckingConnectionSynchronization ? styles.syncIconLoading : ""}>
                                    {"\u21bb"}
                                </span>
                            </button>
                        )}

                        <button
                            className={`${styles.iconButton} ${isDeleteMode ? styles.deleteModeButton : ""}`}
                            type="button"
                            onClick={toggleDeleteMode}
                            disabled={isHomeBusy}
                            aria-label={isDeleteMode ? t("home.cancelDeckDeletion") : t("home.deleteDecks")}
                            aria-pressed={isDeleteMode}
                            title={isDeleteMode ? t("home.cancelDeckDeletion") : t("home.deleteDecks")}
                        >
                            {"\u{1F5D1}"}
                        </button>

                        <button
                            className={styles.iconButton}
                            type="button"
                            aria-label={t("home.trainingStatistics")}
                            title={t("home.trainingStatistics")}
                            disabled={isHomeBusy || isDeleteMode}
                            onClick={() => navigate("/training-statistics")}
                        >
                            <span className={styles.statsIcon} aria-hidden="true">
                                <span></span>
                                <span></span>
                                <span></span>
                            </span>
                        </button>

                        <button
                            className={styles.iconButton}
                            type="button"
                            aria-label={t("home.help")}
                            title={t("home.help")}
                            disabled={isHomeBusy || isDeleteMode}
                            onClick={() => navigate("/help")}
                        >
                            ?
                        </button>

                        <button
                            className={styles.iconButton}
                            type="button"
                            aria-label={t("home.settings")}
                            title={t("home.settings")}
                            disabled={isHomeBusy || isDeleteMode}
                            onClick={() => navigate("/settings", { state: { deckStats } })}
                        >
                            {"\u2699"}
                        </button>
                    </div>
                </div>

                {synchronizationError && <p className={styles.error}>{synchronizationError}</p>}
                {deleteError && <p className={styles.error}>{deleteError}</p>}

                <div className={`${styles.deckStats} ${isHomeBusy ? styles.disabledArea : ""}`}>
                    {!hasLoadedInitialData ? null : !hasDecks ? (
                        <div className={styles.emptyDecks}>
                            <p>
                                {t("home.noDecksBeforeLink")}{" "}
                                <button
                                    className={styles.textLinkButton}
                                    type="button"
                                    onClick={handleAddDeck}
                                    disabled={isHomeBusy || isDeleteMode}
                                >
                                    {t("home.noDecksLink")}
                                </button>{" "}
                                {t("home.noDecksAfterLink")}
                            </p>
                        </div>
                    ) : (
                        <table className={`${styles.deckStatsTable} ${isDeleteMode ? styles.deleteModeTable : ""}`}>
                            <thead>
                                <tr>
                                    <th>{t("home.deck")}</th>
                                    <th>{t("home.words")}</th>
                                    <th>{t("home.today")}</th>
                                    <th>{t("home.weak")}</th>
                                    <th>{t("home.new")}</th>
                                    <th>{t("home.review")}</th>
                                </tr>
                            </thead>
                            <tbody>
                                {deckStats.map(deck => (
                                    <tr
                                        className={`
                                            ${selectedDeckIds.includes(deck.deckId) ? styles.selectedDeckRow : ""}
                                            ${activeDeckMenu?.deckId === deck.deckId ? styles.activeDeckRow : ""}
                                        `}
                                        key={deck.deckId}
                                        onClick={() => {
                                            if (!isHomeBusy) {
                                                handleDeckStatsClick(deck)
                                            }
                                        }}
                                        onContextMenu={(event) => handleDeckContextMenu(event, deck.deckId)}
                                        tabIndex={isHomeBusy ? -1 : 0}
                                        aria-disabled={isHomeBusy}
                                        aria-selected={isDeleteMode && selectedDeckIds.includes(deck.deckId)}
                                    >
                                        <td className={styles.deckNameCell} title={deck.deckName}>
                                            <span className={styles.deckNameText}>{deck.deckName}</span>
                                        </td>
                                        <td>{deck.learnedNotes}</td>
                                        <td className={styles.todayValue}>{getTodayCount(deck)}</td>
                                        <td className={styles.weakValue}>{deck.weakNotes}</td>
                                        <td className={styles.newValue}>{deck.newNotes}</td>
                                        <td className={styles.reviewValue}>{deck.reviewNotes}</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    )}
                </div>

                {activeDeckMenu !== null && !isDeleteMode && (
                    <div
                        className={styles.deckActionMenu}
                        ref={deckMenuRef}
                        style={{ left: activeDeckMenu.x, top: activeDeckMenu.y }}
                        onClick={(event) => event.stopPropagation()}
                    >
                        <button
                            className={`${styles.deckActionButton} ${styles.deckActionDeleteButton}`}
                            type="button"
                            onClick={() => handleDeleteSingleDeck(activeDeckMenu.deckId)}
                            disabled={isHomeBusy}
                        >
                            <span className={styles.trashIcon} aria-hidden="true"></span>
                            <span>{t("home.deleteDeck")}</span>
                        </button>
                    </div>
                )}

                {isDeleteMode && (
                    <div className={styles.deleteActions}>
                        <button
                            className={styles.deleteButton}
                            type="button"
                            onClick={handleDeleteDecks}
                            disabled={selectedDeckIds.length === 0 || isDeletingDecks}
                        >
                            {isDeletingDecks ? t("home.deleting") : t("home.deleteSelected")}
                        </button>
                    </div>
                )}

                {isCheckingConnectionSynchronization && isConnected &&
                    <div className={styles.modalOverlay} role="status" aria-live="polite">
                        <div className={styles.modal}>
                            <div className={styles.spinner}></div>
                            <h2 className={styles.modalTitle}>{t("home.syncRunning")}</h2>
                            <p className={styles.modalText}>{t("home.syncWait")}</p>
                        </div>
                    </div>
                }

            </Canvas>

        </div>
    )
}

export default HomePageCanvas;
