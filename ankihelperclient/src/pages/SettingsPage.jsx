import { useEffect, useMemo, useState } from "react"
import { useLocation, useNavigate } from "react-router-dom"
import ankiHelperApi from "../api/ankiHelperApi"
import Button from "../Components/Button/Button"
import Canvas from "../Components/Canvas/Canvas"
import Title from "../Components/Title/Title"
import { clearTokens } from "../auth/authStorage"
import { useLanguage } from "../i18n/useLanguage"
import { interfaceLanguageOptions } from "../i18n/translations"
import styles from "./SettingsPage.module.css"

const emptySettings = {
    newDeckNames: {},
    deckDailyNewNotesCount: {},
    useOnlyLearnedNotes: {}
}

const SettingsPage = () => {
    const { language, setLanguage, t } = useLanguage()
    const [settings, setSettings] = useState(emptySettings)
    const [interfaceLanguage, setInterfaceLanguage] = useState(language)
    const [isLoading, setIsLoading] = useState(true)
    const [isSaving, setIsSaving] = useState(false)
    const [isLoggingOut, setIsLoggingOut] = useState(false)
    const [account, setAccount] = useState(null)
    const [error, setError] = useState("")
    const [saveMessage, setSaveMessage] = useState("")

    const location = useLocation()
    const navigate = useNavigate()
    const homeDeckStats = useMemo(() => location.state?.deckStats ?? [], [location.state?.deckStats])

    useEffect(() => {
        let shouldIgnoreResult = false

        const loadSettings = async () => {
            setIsLoading(true)
            setError("")

            try {
                const [settingsResult, userResult] = await Promise.all([
                    ankiHelperApi.getSettingsState(),
                    ankiHelperApi.getCurrentUser()
                ])

                if (!shouldIgnoreResult) {
                    setSettings(settingsResult ?? emptySettings)
                    setAccount(userResult)
                }
            } catch (requestError) {
                if (!shouldIgnoreResult) {
                    setError(requestError.message || t("settings.loadFailed"))
                }
            } finally {
                if (!shouldIgnoreResult) {
                    setIsLoading(false)
                }
            }
        }

        loadSettings()

        return () => {
            shouldIgnoreResult = true
        }
    }, [t])

    const deckRows = useMemo(() => {
        const homeDeckIds = homeDeckStats.map(deck => String(deck.deckId))
        const settingDeckIds = Object.keys(settings.newDeckNames ?? {})
        const deckIds = [...new Set([...homeDeckIds, ...settingDeckIds])]

        return deckIds
            .sort((firstDeckId, secondDeckId) => Number(firstDeckId) - Number(secondDeckId))
            .map(deckId => {
                const homeDeck = homeDeckStats.find(deck => String(deck.deckId) === deckId)

                return {
                    id: deckId,
                    name: settings.newDeckNames?.[deckId] ?? homeDeck?.deckName ?? "",
                    dailyNewNotesCount: settings.deckDailyNewNotesCount?.[deckId] ?? 0,
                    useOnlyLearnedNotes: settings.useOnlyLearnedNotes?.[deckId] ?? false
                }
            })
    }, [homeDeckStats, settings])

    const updateDeckName = (deckId, deckName) => {
        setSettings(current => ({
            ...current,
            newDeckNames: {
                ...current.newDeckNames,
                [deckId]: deckName
            }
        }))
        setSaveMessage("")
    }

    const updateDailyNewNotesCount = (deckId, dailyNewNotesCount) => {
        setSettings(current => ({
            ...current,
            deckDailyNewNotesCount: {
                ...current.deckDailyNewNotesCount,
                [deckId]: Math.max(0, Number(dailyNewNotesCount) || 0)
            }
        }))
        setSaveMessage("")
    }

    const updateUseOnlyLearnedNotes = (deckId, useOnlyLearnedNotes) => {
        setSettings(current => ({
            ...current,
            useOnlyLearnedNotes: {
                ...current.useOnlyLearnedNotes,
                [deckId]: useOnlyLearnedNotes
            }
        }))
        setSaveMessage("")
    }

    const updateInterfaceLanguage = (selectedLanguage) => {
        setInterfaceLanguage(selectedLanguage)
        setSaveMessage("")
    }

    const handleSave = async () => {
        setIsSaving(true)
        setError("")
        setSaveMessage("")

        try {
            await ankiHelperApi.updateSettings(settings)
            setLanguage(interfaceLanguage)
            setSaveMessage(t("settings.saved"))
            navigate("/", { replace: true })
        } catch (requestError) {
            setError(requestError.message || t("settings.saveFailed"))
        } finally {
            setIsSaving(false)
        }
    }

    const handleLogout = async () => {
        setIsLoggingOut(true)

        try {
            await ankiHelperApi.logoutUser()
        } finally {
            clearTokens()
            navigate("/login", { replace: true })
        }
    }

    return (
        <div>
            <Canvas>
                <div className={styles.content}>
                    <Title>{t("settings.title")}</Title>

                    {isLoading ? (
                        <p className={styles.statusText}>{t("settings.loading")}</p>
                    ) : error ? (
                        <p className={styles.error}>{error}</p>
                    ) : (
                        <>
                            <section className={styles.accountSettings}>
                                <div>
                                    <h2 className={styles.sectionTitle}>{t("settings.account")}</h2>
                                    <p className={styles.sectionDescription}>{t("settings.accountDescription")}</p>
                                </div>

                                <div className={styles.accountControls}>
                                    <div className={styles.accountEmailField}>
                                        <span>{t("settings.email")}</span>
                                        <strong className={styles.emailText}>{account?.email ?? ""}</strong>
                                    </div>

                                    <Button
                                        className={styles.logoutButton}
                                        onClick={handleLogout}
                                        isDisabled={isLoggingOut}
                                    >
                                        {isLoggingOut ? t("settings.loggingOut") : t("auth.logout")}
                                    </Button>
                                </div>
                            </section>

                            <section className={styles.generalSettings}>
                                <div>
                                    <h2 className={styles.sectionTitle}>{t("settings.general")}</h2>
                                    <p className={styles.sectionDescription}>{t("settings.generalDescription")}</p>
                                </div>

                                <label className={styles.languageField}>
                                    <span>{t("settings.interfaceLanguage")}</span>
                                    <select
                                        className={styles.languageSelect}
                                        value={interfaceLanguage}
                                        onChange={({ target }) => updateInterfaceLanguage(target.value)}
                                    >
                                        {interfaceLanguageOptions.map(option => (
                                            <option key={option.value} value={option.value}>
                                                {option.label}
                                            </option>
                                        ))}
                                    </select>
                                </label>
                            </section>

                            <section className={styles.deckSettings}>
                                <div>
                                    <h2 className={styles.sectionTitle}>{t("settings.deckSettings")}</h2>
                                    <p className={styles.sectionDescription}>{t("settings.deckSettingsDescription")}</p>
                                </div>

                                <div className={styles.settingsTableWrap}>
                                    <table className={styles.settingsTable}>
                                        <thead>
                                            <tr>
                                                <th>{t("settings.deck")}</th>
                                                <th>{t("settings.dailyNew")}</th>
                                                <th>{t("settings.learnedOnly")}</th>
                                            </tr>
                                        </thead>
                                        <tbody>
                                            {deckRows.map(deck => (
                                                <tr key={deck.id}>
                                                    <td>
                                                        <input
                                                            className={styles.nameInput}
                                                            type="text"
                                                            value={deck.name}
                                                            onChange={({ target }) => updateDeckName(deck.id, target.value)}
                                                        />
                                                    </td>
                                                    <td>
                                                        <input
                                                            className={styles.countInput}
                                                            type="number"
                                                            min="0"
                                                            value={deck.dailyNewNotesCount}
                                                            onChange={({ target }) => updateDailyNewNotesCount(deck.id, target.value)}
                                                        />
                                                    </td>
                                                    <td>
                                                        <label className={styles.toggle}>
                                                            <input
                                                                type="checkbox"
                                                                checked={deck.useOnlyLearnedNotes}
                                                                onChange={({ target }) => updateUseOnlyLearnedNotes(deck.id, target.checked)}
                                                            />
                                                            <span className={styles.toggleTrack}></span>
                                                        </label>
                                                    </td>
                                                </tr>
                                            ))}
                                        </tbody>
                                    </table>
                                </div>
                            </section>

                            {saveMessage && <p className={styles.statusText}>{saveMessage}</p>}
                        </>
                    )}

                    <div className={styles.actions}>
                        <Button
                            onClick={handleSave}
                            isDisabled={isLoading || isSaving}
                        >
                            {isSaving ? t("settings.saving") : t("settings.save")}
                        </Button>
                    </div>
                </div>
            </Canvas>
        </div>
    )
}

export default SettingsPage
