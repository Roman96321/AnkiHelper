import { useState, useEffect, useRef } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import Button from "../../Components/Button/Button";
import Canvas from "../../Components/Canvas/Canvas";
import Title from "../../Components/Title/Title";
import ankiHelperApi from "../../api/ankiHelperApi";
import styles from "./ConfigureDecksCanvas.module.css";
import { useLanguage } from "../../i18n/useLanguage";

const ConfigureDecksCanvas = () => {
    const [isLoadingDecks, setIsLoadingDecks] = useState(true);
    const [configuration, setConfiguration] = useState([]);
    const [selectedFields, setSelectedFields] = useState({});
    const [isImporting, setIsImporting] = useState(false);
    const [error, setError] = useState("");
    const { t } = useLanguage();
    const location = useLocation();
    const hasRequestedConfiguration = useRef(false);

    const selectedDeckNames = location.state?.selectedDeckNames;

    const navigate = useNavigate();

    const getNoteSelectionKey = (deckName, noteId) => {
        return `${deckName}:${noteId}`;
    };
    
    useEffect(() => {
        const loadDeckConfiguration = async () => {
            if (hasRequestedConfiguration.current) {
                return;
            }

            hasRequestedConfiguration.current = true;

            if (!selectedDeckNames) {
                setError(t("configure.decksNotSelected"));
                setIsLoadingDecks(false);
                return;
            }

            try {
                const result = await ankiHelperApi.postDecksName(selectedDeckNames);
                setConfiguration(result);
            } catch {
                setError(t("configure.loadFailed"));
            } finally {
                setIsLoadingDecks(false);
            }
        }

        loadDeckConfiguration();
    }, [selectedDeckNames, t])   

    const handleFieldSelectionChange = (deckName, noteId, fieldName, targetField) => {
        const noteSelectionKey = getNoteSelectionKey(deckName, noteId);
        const oppositeField = targetField === "front" ? "back" : "front";

        setSelectedFields(currentSelectedFields => {
            const currentNoteSelection = currentSelectedFields[noteSelectionKey] ?? {};
            const previousTargetField = currentNoteSelection[targetField];
            const isOppositeFieldSelected = currentNoteSelection[oppositeField] === fieldName;

            return {
                ...currentSelectedFields,
                [noteSelectionKey]: {
                    ...currentNoteSelection,
                    [targetField]: fieldName,
                    [oppositeField]: isOppositeFieldSelected
                        ? previousTargetField
                        : currentNoteSelection[oppositeField]
                }
            };
        });
    };

    const handleSubmit = async () => {
        const importConfiguration = configuration
            .filter(deck => deck.notes.length > 0)
            .map(deck => ({
            deckName: deck.name,
            noteTypes: deck.notes.map(note => {
                const noteSelection = selectedFields[getNoteSelectionKey(deck.name, note.id)];

                return {
                    frontKey: noteSelection.front,
                    backKey: noteSelection.back
                };
            })
        }));

        setIsImporting(true);
        setError("");

        try {
            await ankiHelperApi.postImportConfiguration(importConfiguration);
            navigate("/");
        } catch (error) {
            setError(error.message || t("configure.importFailed"));
        } finally {
            setIsImporting(false);
        }
    };

    const importableConfiguration = configuration.filter(deck => deck.notes.length > 0);

    const isConfigurationComplete = importableConfiguration.length > 0 &&
        importableConfiguration.every(deck => deck.notes.every(note => {
            const noteSelection = selectedFields[getNoteSelectionKey(deck.name, note.id)];

            return noteSelection?.front && noteSelection?.back;
        }));

    return (
        <div>
            <Canvas>
                {isLoadingDecks ? <Title>{t("configure.loading")}</Title> :
                    <div className={styles.content}>
                        <Title>{t("configure.title")}</Title>
                        <p className={styles.hint}>
                            {t("configure.hint")}
                        </p>
                        {error ? <p className={styles.error}>{error}</p> :
                            <div className={styles.deckList}>
                                {configuration.map(deck => (
                                    <section className={styles.deck} key={deck.name}>
                                        <h2 className={styles.deckTitle}>{deck.name}</h2>

                                        {deck.notes.length === 0 &&
                                            <p className={styles.error}>{t("configure.noNotes")}</p>
                                        }

                                        {deck.notes.map(note => (
                                            <div className={styles.note} key={note.id}>
                                                {deck.notes.length > 1 &&
                                                    <h3 className={styles.noteTitle}>{t("configure.noteType", { id: note.id })}</h3>
                                                }

                                                    <div className={styles.table}>
                                                        <div className={styles.headerCell}></div>
                                                    <div className={styles.headerCell}>{t("configure.front")}</div>
                                                    <div className={styles.headerCell}>{t("configure.back")}</div>

                                                    {Object.entries(note.fields).map(([fieldName, fieldValue]) => {
                                                        const noteSelection = selectedFields[getNoteSelectionKey(deck.name, note.id)];

                                                        return (
                                                            <div className={styles.row} key={fieldName}>
                                                            <div className={styles.fieldPreview}>
                                                                <span className={styles.fieldName}>{fieldName}:</span>
                                                                <span>{fieldValue}</span>
                                                            </div>

                                                            <label className={styles.radioCell}>
                                                                <input
                                                                    type="radio"
                                                                    name={`${getNoteSelectionKey(deck.name, note.id)}:front`}
                                                                    checked={noteSelection?.front === fieldName}
                                                                    onChange={() => handleFieldSelectionChange(deck.name, note.id, fieldName, "front")}
                                                                />
                                                            </label>

                                                            <label className={styles.radioCell}>
                                                                <input
                                                                    type="radio"
                                                                    name={`${getNoteSelectionKey(deck.name, note.id)}:back`}
                                                                    checked={noteSelection?.back === fieldName}
                                                                    onChange={() => handleFieldSelectionChange(deck.name, note.id, fieldName, "back")}
                                                                />
                                                            </label>
                                                        </div>
                                                        )
                                                    })}
                                                </div>
                                            </div>
                                        ))}
                                    </section>
                                ))}
                            </div>
                        }

                        <div className={styles.actions}>
                            <Button
                                onClick={handleSubmit}
                                isDisabled={configuration.length === 0 || !isConfigurationComplete || isImporting}
                            >
                                {isImporting ? t("configure.importing") : t("configure.import")}
                            </Button>
                        </div>
                    </div>               
                }
            </Canvas>
        </div>
    
    )
}

export default ConfigureDecksCanvas;
