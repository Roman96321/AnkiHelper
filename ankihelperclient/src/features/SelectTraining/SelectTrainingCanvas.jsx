import Canvas from "../../Components/Canvas/Canvas"
import Title from "../../Components/Title/Title"
import Button from "../../Components/Button/Button"
import { useLocation, useNavigate } from "react-router-dom"
import { useState } from "react"
import styles from "./SelectTrainingCanvas.module.css"
import { useLanguage } from "../../i18n/useLanguage"

const trainingModes = [
    {
        id: "word-order",
        route: "/training/word-order",
        titleKey: "trainingModes.wordOrder.title",
        descriptionKeys: ["trainingModes.wordOrder.description1", "trainingModes.wordOrder.description2"]
    },
    {
        id: "translation-sentence",
        route: "/training/translation-sentence",
        titleKey: "trainingModes.translationSentence.title",
        descriptionKeys: ["trainingModes.translationSentence.description1", "trainingModes.translationSentence.description2"]
    },
    {
        id: "sentence-composition",
        route: "/training/sentence-composition",
        titleKey: "trainingModes.sentenceComposition.title",
        descriptionKeys: ["trainingModes.sentenceComposition.description1", "trainingModes.sentenceComposition.description2"]
    }
]

const SelectTrainingCanvas = () => {
    const [selectedModeId, setSelectedModeId] = useState("")
    const { t } = useLanguage()
    const navigate = useNavigate()
    const location = useLocation()

    const deckId = location.state?.id
    const selectedMode = trainingModes.find(mode => mode.id === selectedModeId)

    const handleStartTraining = () => {
        if (!selectedMode) {
            return
        }

        navigate(selectedMode.route, { state: { deckId } })
    }

    return (
        <div>
            <Canvas>
                <Title>{t("selectTraining.title")}</Title>

                <div className={styles.content}>
                    <div className={styles.modeSelector} role="group" aria-label={t("selectTraining.aria")}>
                        {trainingModes.map(mode => (
                            <button
                                className={`${styles.modeButton} ${selectedModeId === mode.id ? styles.selectedModeButton : ""}`}
                                key={mode.id}
                                type="button"
                                aria-pressed={selectedModeId === mode.id}
                                onClick={() => setSelectedModeId(mode.id)}
                            >
                                {t(mode.titleKey)}
                            </button>
                        ))}
                    </div>

                    <div className={styles.description}>
                        {selectedMode ? (
                            selectedMode.descriptionKeys.map(key => (
                                <p key={key}>{t(key)}</p>
                            ))
                        ) : (
                            <p>{t("selectTraining.emptyDescription")}</p>
                        )}
                    </div>
                </div>

                <div className={styles.actions}>
                    <Button
                        onClick={handleStartTraining}
                        isDisabled={!selectedMode}
                    >
                        {t("selectTraining.start")}
                    </Button>
                </div>
            </Canvas>
        </div>
    )
}

export default SelectTrainingCanvas
