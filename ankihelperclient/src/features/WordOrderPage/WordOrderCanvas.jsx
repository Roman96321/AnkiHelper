import Canvas from "../../Components/Canvas/Canvas"
import Title from "../../Components/Title/Title"
import Button from "../../Components/Button/Button"
import ankiHelperApi from "../../api/ankiHelperApi"
import { useEffect, useMemo, useState } from "react"
import { useLocation, useNavigate } from "react-router-dom"
import styles from "./WordOrderCanvas.module.css"
import { useLanguage } from "../../i18n/useLanguage"

const normalizeSentence = (sentence) =>
    sentence
        .trim()
        .replace(/\s+/g, " ")
        .toLowerCase()

const createWordItems = (words = []) =>
    words.map((word, index) => ({
        id: `${index}-${word}`,
        text: word
    }))


const WordOrderCanvas = () => {
    const location = useLocation()
    const navigate = useNavigate()
    const { t } = useLanguage()
    const deckId = location.state?.deckId

    const [question, setQuestion] = useState(null)
    const [nextQuestion, setNextQuestion] = useState(null)
    const [answerWords, setAnswerWords] = useState([])
    const [availableWords, setAvailableWords] = useState([])
    const [draggedWord, setDraggedWord] = useState(null)
    const [result, setResult] = useState(null)
    const [isLoading, setIsLoading] = useState(false)
    const [error, setError] = useState("")
    const [isTrainingComplete, setIsTrainingComplete] = useState(false)
    const [remainingNotesCount, setRemainingNotesCount] = useState()

    const userSentence = useMemo(
        () => answerWords.map(word => word.text).join(" "),
        [answerWords]
    )

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

                return await ankiHelperApi.postTrainingWordOrder({ deckId })
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
                    setAnswerWords([])
                    setAvailableWords([])
                    setResult(null)
                    setIsTrainingComplete(true)
                    setRemainingNotesCount(0)
                    return
                }

                setQuestion(loadedQuestion)
                setRemainingNotesCount(loadedQuestion.remainingCount)
                setNextQuestion(null)
                setAnswerWords([])
                setAvailableWords(createWordItems(loadedQuestion?.words ?? []))
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

    const moveWordToAnswer = (word, insertIndex = answerWords.length) => {
        if (!word || result) {
            return
        }

        setAvailableWords(current => current.filter(item => item.id !== word.id))
        setAnswerWords(current => {
            const withoutDragged = current.filter(item => item.id !== word.id)
            const boundedIndex = Math.min(insertIndex, withoutDragged.length)

            return [
                ...withoutDragged.slice(0, boundedIndex),
                word,
                ...withoutDragged.slice(boundedIndex)
            ]
        })
    }

    const moveWordToBank = (word) => {
        if (!word || result) {
            return
        }

        setAnswerWords(current => current.filter(item => item.id !== word.id))
        setAvailableWords(current => current.some(item => item.id === word.id) ? current : [...current, word])
    }

    const handleDragStart = (word) => {
        setDraggedWord(word)
    }

    const allowDrop = (event) => {
        event.preventDefault()
    }

    const handleAnswerDrop = () => {
        moveWordToAnswer(draggedWord)
        setDraggedWord(null)
    }

    const handleAnswerWordDrop = (event, targetIndex) => {
        event.stopPropagation()
        moveWordToAnswer(draggedWord, targetIndex)
        setDraggedWord(null)
    }

    const handleBankDrop = () => {
        moveWordToBank(draggedWord)
        setDraggedWord(null)
    }

    const handleCheckAnswer = async () => {
        if (!question || answerWords.length === 0) {
            return
        }

        const isCorrect = normalizeSentence(userSentence) === normalizeSentence(question.correctSentence)

        setResult({
            isCorrect,
            message: isCorrect ? t("wordOrder.correctAnswer") : t("wordOrder.notQuiteRight"),
            correctSentence: question.correctSentence
        })

        setIsLoading(true)
        setError("")

        try {
            const loadedNextQuestion = await ankiHelperApi.postTrainingWordOrder({
                deckId,
                noteId: question.noteId,
                isCorrectAnswer: isCorrect
            })

            if (!loadedNextQuestion) {
                setNextQuestion(null)
                setIsTrainingComplete(true)
                setRemainingNotesCount(0)
                return
            }

            setNextQuestion(loadedNextQuestion)
            setRemainingNotesCount(loadedNextQuestion.remainingCount)
            setIsTrainingComplete(false)
        } catch (requestError) {
            setError(requestError.message || t("common.failedLoadNextQuestion"))
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
        setAnswerWords([])
        setAvailableWords(createWordItems(nextQuestion.words ?? []))
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
                    <Title>{t("trainingModes.wordOrder.title")}</Title>
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
                            <p>{t("wordOrder.noQuestions")}</p>
                        </section>
                    )}

                    {question && (
                        <>
                            <section
                                className={`${styles.answerZone} ${answerWords.length === 0 ? styles.emptyAnswerZone : ""}`}
                                onDragOver={allowDrop}
                                onDrop={handleAnswerDrop}
                            >
                                {answerWords.length === 0 ? (
                                    <span className={styles.placeholder}>{t("wordOrder.dragWordsHere")}</span>
                                ) : (
                                    answerWords.map((word, index) => (
                                        <button
                                            className={styles.wordChip}
                                            draggable={!result}
                                            key={word.id}
                                            type="button"
                                            onClick={() => moveWordToBank(word)}
                                            onDragStart={() => handleDragStart(word)}
                                            onDragOver={allowDrop}
                                            onDrop={(event) => handleAnswerWordDrop(event, index)}
                                        >
                                            {word.text}
                                        </button>
                                    ))
                                )}
                            </section>

                            <section className={styles.translationSection}>
                                <p className={styles.translation}>{question.translation}</p>
                            </section>

                            {result && (
                                <section className={`${styles.result} ${result.isCorrect ? styles.correct : styles.incorrect}`}>
                                    <p>{result.message}</p>
                                    {!result.isCorrect && <p>{t("wordOrder.correct", { sentence: result.correctSentence })}</p>}
                                    {isTrainingComplete && <p>{t("common.lastAvailableQuestion")}</p>}
                                </section>
                            )}

                            <section
                                className={styles.wordBank}
                                onDragOver={allowDrop}
                                onDrop={handleBankDrop}
                            >
                                {availableWords.map(word => (
                                    <button
                                        className={styles.wordChip}
                                        draggable={!result}
                                        key={word.id}
                                        type="button"
                                        onClick={() => moveWordToAnswer(word)}
                                        onDragStart={() => handleDragStart(word)}
                                    >
                                        {word.text}
                                    </button>
                                ))}
                            </section>
                        </>
                    )}
                </div>

                <div className={styles.actions}>
                    {!result ? (
                        <Button
                            onClick={handleCheckAnswer}
                            isDisabled={!question || answerWords.length === 0 || isLoading}
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

export default WordOrderCanvas
