import Canvas from "../../Components/Canvas/Canvas"
import Title from "../../Components/Title/Title"
import Button from "../../Components/Button/Button"
import ankiHelperApi from "../../api/ankiHelperApi"
import { useEffect, useState } from "react"
import { useLocation, useNavigate } from "react-router-dom"
import styles from "./TranslationSentenceCanvas.module.css"
import { useLanguage } from "../../i18n/useLanguage"

const TranslationSentenceCanvas = () => {
    const location = useLocation()
    const navigate = useNavigate()
    const { t } = useLanguage()
    const deckId = location.state?.deckId

    const [question, setQuestion] = useState(null)
    const [nextQuestion, setNextQuestion] = useState(null)
    const [answerSentence, setAnswerSentence] = useState("")
    const [result, setResult] = useState(null)
    const [isLoading, setIsLoading] = useState(false)
    const [error, setError] = useState("")
    const [isTrainingComplete, setIsTrainingComplete] = useState(false)
    const [remainingNotesCount, setRemainingNotesCount] = useState()

    useEffect(() => {
        let shouldIgnoreResult = false

        Promise.resolve()
            .then(async () => {
                if (!deckId) {
                    return null
                }

                if (!shouldIgnoreResult) {
                    setIsLoading(true)
                    setError("")
                }

                return await ankiHelperApi.postTranslationSentence({ deckId })
            })
            .then(loadedQuestion => {
                if (shouldIgnoreResult) {
                    return
                }

                if (!deckId) {
                    setQuestion(null)
                    setError(t("common.deckNotSelected"))
                    return
                }

                if (!loadedQuestion) {
                    setQuestion(null)
                    setNextQuestion(null)
                    setAnswerSentence("")
                    setResult(null)
                    setIsTrainingComplete(true)
                    setRemainingNotesCount(0)
                    return
                }

                setQuestion(loadedQuestion)
                setRemainingNotesCount(loadedQuestion.remainingCount)
                setNextQuestion(null)
                setAnswerSentence("")
                setResult(null)
                setIsTrainingComplete(false)
            })
            .catch(requestError => {
                if (!shouldIgnoreResult) {
                    setError(requestError.message || t("common.failedLoadTrainingQuestion"))
                }
            })
            .finally(() => {
                if (!shouldIgnoreResult) {
                    setIsLoading(false)
                }
            })

        return () => {
            shouldIgnoreResult = true
        }
    }, [deckId, t])

    const handleCheckAnswer = async () => {
        const trimmedAnswer = answerSentence.trim()

        if (!question || !trimmedAnswer) {
            return
        }

        setIsLoading(true)
        setError("")

        try {
            const response = await ankiHelperApi.postTranslationSentence({
                deckId,
                noteId: question.noteId,
                answerSentence: trimmedAnswer,
                questionSentence: question.questionSentence
            })

            if (!response) {
                setResult(null)
                setNextQuestion(null)
                setIsTrainingComplete(true)
                setRemainingNotesCount(0)
                return
            }

            setResult({
                isCorrect: response.isCorrect,
                verdict: response.verdict,
                moreCorrectSentence: response.moreCorrectSentence
            })

            setRemainingNotesCount(response.remainingCount)

            if (response.noteId && response.questionSentence) {
                setNextQuestion(response)
                setIsTrainingComplete(false)
                return
            }

            setNextQuestion(null)
            setIsTrainingComplete(true)
        } catch (requestError) {
            setError(requestError.message || t("common.failedCheckAnswer"))
        } finally {
            setIsLoading(false)
        }
    }

    const handleNextQuestion = () => {
        if (!nextQuestion) {
            return
        }

        setQuestion(nextQuestion)
        setRemainingNotesCount(nextQuestion.remainingCount)
        setNextQuestion(null)
        setAnswerSentence("")
        setResult(null)
        setError("")
        setIsTrainingComplete(false)
    }

    const handleBack = () => {
        navigate("/select-training", { state: { id: deckId } })
    }

    const handleFinishTraining = () => {
        navigate("/")
    }

    return (
        <div>
            <Canvas>
                <div className={styles.header}>
                    <Title>{t("trainingModes.translationSentence.title")}</Title>
                    <p className={styles.remainingCounter}>
                        {remainingNotesCount !== undefined ? t("common.remaining", { count: remainingNotesCount }) : ""}
                    </p>
                    <button className={styles.backButton} type="button" onClick={handleBack}>
                        {t("common.back")}
                    </button>
                </div>

                <div className={styles.content}>
                    {error && <p className={styles.error}>{error}</p>}

                    {isLoading && !question && <p className={styles.status}>{t("common.loadingQuestion")}</p>}

                    {isTrainingComplete && !question && !isLoading && (
                        <section className={styles.completed}>
                            <h2>{t("common.trainingComplete")}</h2>
                            <p>{t("translationSentence.noQuestions")}</p>
                        </section>
                    )}

                    {question && (
                        <>
                            <section className={styles.composeArea}>
                                <textarea
                                    className={styles.answerInput}
                                    value={answerSentence}
                                    disabled={Boolean(result)}
                                    placeholder={t("translationSentence.writeTranslation")}
                                    onChange={event => setAnswerSentence(event.target.value)}
                                />
                            </section>

                            <section className={styles.questionSentence}>
                                <p>{question.questionSentence}</p>
                            </section>

                            {result && (
                                <section className={`${styles.result} ${result.isCorrect ? styles.correct : styles.incorrect}`}>
                                    <p>{result.verdict}</p>
                                    {result.moreCorrectSentence && (
                                        <p>{t("common.better", { sentence: result.moreCorrectSentence })}</p>
                                    )}
                                    {isTrainingComplete && <p>{t("common.lastAvailableQuestion")}</p>}
                                </section>
                            )}
                        </>
                    )}
                </div>

                <div className={styles.actions}>
                    {!result ? (
                        <Button
                            onClick={handleCheckAnswer}
                            isDisabled={!question || !answerSentence.trim() || isLoading}
                        >
                            {isLoading ? t("common.checking") : t("common.checkAnswer")}
                        </Button>
                    ) : isTrainingComplete ? (
                        <Button
                            onClick={handleFinishTraining}
                            isDisabled={isLoading}
                        >
                            {t("common.finishTraining")}
                        </Button>
                    ) : (
                        <Button
                            onClick={handleNextQuestion}
                            isDisabled={!nextQuestion || isLoading}
                        >
                            {isLoading ? t("common.loading") : t("common.nextQuestion")}
                        </Button>
                    )}
                </div>
            </Canvas>
        </div>
    )
}

export default TranslationSentenceCanvas
