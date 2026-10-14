import { useCallback, useEffect, useMemo, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import Canvas from "../../Components/Canvas/Canvas";
import Button from "../../Components/Button/Button";
import DeckNameItem from "../../Components/DeckNameItem/DeckNameItem";
import ankiHelperApi from "../../api/ankiHelperApi";
import styles from "./AddDecksPageCanvas.module.css";
import Title from "../../Components/Title/Title";
import { useLanguage } from "../../i18n/useLanguage";

const AddDecksPageCanvas = () => {
    const navigate = useNavigate();
    const location = useLocation();
    const { t } = useLanguage();
    const routeExistingDeckIds = location.state?.existingDeckIds;
    const routeAnkiSource = location.state?.ankiSource;

    const [isLoadingDecks, setIsLoadingDecks] = useState(false);
    const [isLoadingSource, setIsLoadingSource] = useState(!routeAnkiSource);
    const [isUploadingPackage, setIsUploadingPackage] = useState(false);
    const [uploadError, setUploadError] = useState("");
    const [deckNames, setDeckNames] = useState([]);
    const [selectedDeckNames, setSelectedDeckNames] = useState([]);
    const [uploadedPackageName, setUploadedPackageName] = useState("");
    const [ankiSource, setAnkiSource] = useState(routeAnkiSource ?? "");
    const existingDeckIds = useMemo(
        () => routeExistingDeckIds ?? [],
        [routeExistingDeckIds]
    );
    const isApkgSource = ankiSource === "Apkg";

    const applyDeckNames = useCallback((decksByName) => {
        const decks = Object.entries(decksByName).map(([name, id]) => ({
            id,
            name
        }));
        const newDecks = decks
            .filter(deck => !existingDeckIds.includes(deck.id))
            .map(deck => deck.name);

        setDeckNames(decks);
        setSelectedDeckNames(newDecks);
    }, [existingDeckIds]);

    useEffect(() => {
        let shouldIgnoreResult = false;

        const loadAnkiSource = async () => {
            if (routeAnkiSource) {
                setAnkiSource(routeAnkiSource);
                setIsLoadingSource(false);
                return;
            }

            setIsLoadingSource(true);

            try {
                const source = await ankiHelperApi.getAnkiSource();

                if (!shouldIgnoreResult) {
                    setAnkiSource(source);
                }
            } finally {
                if (!shouldIgnoreResult) {
                    setIsLoadingSource(false);
                }
            }
        };

        loadAnkiSource();

        return () => {
            shouldIgnoreResult = true;
        };
    }, [routeAnkiSource]);

    useEffect(() => {
        let shouldIgnoreResult = false;

        const loadDeckNames = async () => {
            setIsLoadingDecks(true);

            try {
                const decksByName = await ankiHelperApi.getDecksName();

                if (!shouldIgnoreResult) {
                    applyDeckNames(decksByName);
                }
            } finally {
                if (!shouldIgnoreResult) {
                    setIsLoadingDecks(false);
                }
            }
        };

        if (ankiSource && !isApkgSource) {
            loadDeckNames();
        }

        return () => {
            shouldIgnoreResult = true;
        };
    }, [ankiSource, applyDeckNames, isApkgSource]);

    const handlePackageFileChange = async ({ target }) => {
        const file = target.files?.[0];

        if (!file) {
            return;
        }

        setIsUploadingPackage(true);
        setUploadError("");
        setUploadedPackageName("");
        setDeckNames([]);
        setSelectedDeckNames([]);

        try {
            await ankiHelperApi.uploadAnkiApkgFile(file);
            const decksByName = await ankiHelperApi.getDecksName();

            applyDeckNames(decksByName);
            setUploadedPackageName(file.name);
        } catch (error) {
            setUploadError(error.message || t("addDecks.uploadFailed"));
        } finally {
            setIsUploadingPackage(false);
            target.value = "";
        }
    };

    const handleDeckSelectionChange = (deckName, isSelected) => {
        setSelectedDeckNames(currentDeckNames => {
            if (isSelected) {
                return [...currentDeckNames, deckName];
            }

            return currentDeckNames.filter(currentDeckName => currentDeckName !== deckName);
        });
    };

    const handleNextStep = async () => {
        navigate("/configure-decks", { state: { selectedDeckNames } });
    }

    return (
        <div>
            <Canvas>
                {isLoadingSource || isLoadingDecks ? <Title>{t("addDecks.loadingDeck")}</Title> :
                    isApkgSource && !uploadedPackageName ?
                        <div className={styles.content}>
                            <Title>{t("addDecks.importPackage")}</Title>

                            <div className={styles.uploadPanel}>
                                <p className={styles.uploadText}>{t("addDecks.uploadText")}</p>
                                <p className={styles.helpText}>
                                    {t("addDecks.importHelpBeforeLink")}{" "}
                                    <button
                                        className={styles.textLinkButton}
                                        type="button"
                                        onClick={() => navigate("/help")}
                                    >
                                        {t("addDecks.importHelpLink")}
                                    </button>
                                    .
                                </p>

                                <label className={styles.fileUploadLabel}>
                                    <span className={styles.fileUploadTitle}>
                                        {isUploadingPackage ? t("addDecks.uploading") : t("addDecks.selectApkg")}
                                    </span>
                                    <input
                                        className={styles.fileInput}
                                        type="file"
                                        accept=".apkg"
                                        onChange={handlePackageFileChange}
                                        disabled={isUploadingPackage}
                                    />
                                </label>

                                {isUploadingPackage && <p className={styles.statusText}>{t("addDecks.readingPackage")}</p>}
                                {uploadError && <p className={styles.error}>{uploadError}</p>}
                            </div>
                        </div> :
                    <div className={styles.content}>
                        <Title>{t("addDecks.chooseDecks")}</Title>
                        {uploadedPackageName && <p className={styles.statusText}>{uploadedPackageName}</p>}
                        <p className={styles.helpText}>
                            {t("addDecks.importHelpBeforeLink")}{" "}
                            <button
                                className={styles.textLinkButton}
                                type="button"
                                onClick={() => navigate("/help")}
                            >
                                {t("addDecks.importHelpLink")}
                            </button>
                            .
                        </p>

                        <br></br>

                        <ul className={styles.deckList}>
                            {deckNames.map(deck => (
                                <DeckNameItem
                                    key={deck.id}
                                    name={deck.name}
                                    isAlreadyAdded={existingDeckIds.includes(deck.id)}
                                    isSelected={selectedDeckNames.includes(deck.name)}
                                    onSelectionChange={handleDeckSelectionChange}
                                />
                            ))}
                        </ul>

                        <div className={styles.actions}>
                            <Button
                                isDisabled={selectedDeckNames.length === 0}
                                onClick={handleNextStep}
                            >
                                {t("addDecks.nextStep")}
                            </Button>
                        </div>
                    </div>
                }
            </Canvas>
        </div>
    )
}

export default AddDecksPageCanvas;
