import { useEffect, useMemo, useRef, useState } from "react"
import ankiHelperApi from "../../api/ankiHelperApi"
import Canvas from "../../Components/Canvas/Canvas"
import Title from "../../Components/Title/Title"
import { useLanguage } from "../../i18n/useLanguage"
import styles from "./TrainingStatisticsCanvas.module.css"

const languageLocales = {
    en: "en-US",
    pl: "pl-PL",
    uk: "uk-UA"
}

const padDatePart = (value) => String(value).padStart(2, "0")

const getDateKey = (date) => {
    return [
        date.getFullYear(),
        padDatePart(date.getMonth() + 1),
        padDatePart(date.getDate())
    ].join("-")
}

const getDaysInYear = (year) => {
    const days = []
    const date = new Date(year, 0, 1)

    while (date.getFullYear() === year) {
        days.push(new Date(date))
        date.setDate(date.getDate() + 1)
    }

    return days
}

const getIntensityLevel = (count, maxCount) => {
    if (count <= 0 || maxCount <= 0) {
        return 0
    }

    return Math.max(1, Math.ceil((count / maxCount) * 4))
}

const formatShortDate = (date) => {
    return [
        padDatePart(date.getDate()),
        padDatePart(date.getMonth() + 1),
        date.getFullYear()
    ].join(".")
}

const calculateYearMetrics = (yearDays, statisticsByDate, selectedYear) => {
    let totalNotes = 0
    let activeDays = 0
    let bestStreak = 0
    let currentStreak = 0
    let runningStreak = 0
    let bestDayCount = 0
    let bestDay = null

    yearDays.forEach(date => {
        const dateKey = getDateKey(date)
        const count = Number(statisticsByDate[dateKey] ?? 0)

        totalNotes += count

        if (count > 0) {
            activeDays++
            runningStreak++

            if (count > bestDayCount) {
                bestDayCount = count
                bestDay = date
            }
        } else {
            runningStreak = 0
        }

        bestStreak = Math.max(bestStreak, runningStreak)
    })

    const today = new Date()
    const currentYear = today.getFullYear()
    const streakEndDate = selectedYear === currentYear
        ? today
        : yearDays.at(-1)
    const streakDays = selectedYear <= currentYear
        ? yearDays.filter(date => date <= streakEndDate)
        : []

    for (let index = streakDays.length - 1; index >= 0; index--) {
        const count = Number(statisticsByDate[getDateKey(streakDays[index])] ?? 0)

        if (count <= 0) {
            break
        }

        currentStreak++
    }

    return {
        totalNotes,
        activeDays,
        completionPercent: yearDays.length > 0 ? Math.round((activeDays / yearDays.length) * 100) : 0,
        bestStreak,
        currentStreak,
        bestDay,
        bestDayCount,
        averagePerActiveDay: activeDays > 0 ? totalNotes / activeDays : 0,
        averagePerCalendarDay: yearDays.length > 0 ? totalNotes / yearDays.length : 0
    }
}

const TrainingStatisticsCanvas = () => {
    const { language, t } = useLanguage()
    const [selectedYear, setSelectedYear] = useState(new Date().getFullYear())
    const [statisticsByDate, setStatisticsByDate] = useState({})
    const [isLoading, setIsLoading] = useState(true)
    const [error, setError] = useState("")
    const [hoveredDay, setHoveredDay] = useState(null)
    const [canUseDayTooltips, setCanUseDayTooltips] = useState(true)
    const tooltipRef = useRef(null)

    useEffect(() => {
        let shouldIgnoreResult = false

        const loadStatistics = async () => {
            setIsLoading(true)
            setError("")

            try {
                const result = await ankiHelperApi.getTrainingStatistics()

                if (!shouldIgnoreResult) {
                    setStatisticsByDate(result ?? {})
                }
            } catch (requestError) {
                if (!shouldIgnoreResult) {
                    setError(requestError.message || t("trainingStatistics.loadFailed"))
                }
            } finally {
                if (!shouldIgnoreResult) {
                    setIsLoading(false)
                }
            }
        }

        loadStatistics()

        return () => {
            shouldIgnoreResult = true
        }
    }, [t])

    useEffect(() => {
        const updateCanUseDayTooltips = () => {
            setCanUseDayTooltips(!window.matchMedia("(hover: none), (max-width: 640px)").matches)
        }

        updateCanUseDayTooltips()
        window.addEventListener("resize", updateCanUseDayTooltips)

        return () => {
            window.removeEventListener("resize", updateCanUseDayTooltips)
        }
    }, [])

    const yearDays = useMemo(() => getDaysInYear(selectedYear), [selectedYear])

    const yearsWithStatistics = useMemo(() => {
        return new Set(
            Object.entries(statisticsByDate)
                .filter(([, count]) => Number(count) > 0)
                .map(([dateKey]) => Number(dateKey.slice(0, 4)))
        )
    }, [statisticsByDate])

    const previousYearWithStatistics = useMemo(() => {
        return [...yearsWithStatistics]
            .filter(year => year < selectedYear)
            .sort((firstYear, secondYear) => secondYear - firstYear)[0]
    }, [selectedYear, yearsWithStatistics])

    const nextYearWithStatistics = useMemo(() => {
        return [...yearsWithStatistics]
            .filter(year => year > selectedYear)
            .sort((firstYear, secondYear) => firstYear - secondYear)[0]
    }, [selectedYear, yearsWithStatistics])

    const canMoveToPreviousYear = previousYearWithStatistics !== undefined
    const canMoveToNextYear = nextYearWithStatistics !== undefined

    const maxDailyCount = useMemo(() => {
        return yearDays.reduce((maxCount, date) => {
            const dateKey = getDateKey(date)
            const count = Number(statisticsByDate[dateKey] ?? 0)

            return Math.max(maxCount, count)
        }, 0)
    }, [statisticsByDate, yearDays])

    const dateFormatter = useMemo(() => {
        return new Intl.DateTimeFormat(languageLocales[language] ?? languageLocales.en, {
            day: "numeric",
            month: "long",
            year: "numeric"
        })
    }, [language])

    const numberFormatter = useMemo(() => {
        return new Intl.NumberFormat(languageLocales[language] ?? languageLocales.en, {
            maximumFractionDigits: 1
        })
    }, [language])

    const yearMetrics = useMemo(
        () => calculateYearMetrics(yearDays, statisticsByDate, selectedYear),
        [selectedYear, statisticsByDate, yearDays]
    )

    const moveToYear = (year) => {
        if (year !== undefined) {
            setSelectedYear(year)
        }
    }

    const showDayTooltip = (formattedDate, count, x, y) => {
        setHoveredDay({
            date: formattedDate,
            notes: count > 0
                ? t("trainingStatistics.notesLearned", { count })
                : t("trainingStatistics.noNotesLearned"),
            x,
            y
        })
    }

    const showDayTooltipFromMouse = (event, formattedDate, count) => {
        showDayTooltip(formattedDate, count, event.clientX, event.clientY)
    }

    const showDayTooltipFromFocus = (event, formattedDate, count) => {
        const rect = event.currentTarget.getBoundingClientRect()
        showDayTooltip(formattedDate, count, rect.left + rect.width / 2, rect.top)
    }

    const moveDayTooltip = (event) => {
        setHoveredDay(current => current
            ? {
                ...current,
                x: event.clientX,
                y: event.clientY
            }
            : current)
    }

    const hideDayTooltip = () => {
        setHoveredDay(null)
    }

    useEffect(() => {
        if (!hoveredDay) {
            return
        }

        const closeTooltipOnOutsidePointerDown = (event) => {
            if (
                event.target.closest?.(`.${styles.dayCell}`) ||
                tooltipRef.current?.contains(event.target)
            ) {
                return
            }

            hideDayTooltip()
        }

        document.addEventListener("pointerdown", closeTooltipOnOutsidePointerDown)

        return () => {
            document.removeEventListener("pointerdown", closeTooltipOnOutsidePointerDown)
        }
    }, [hoveredDay])

    return (
        <div>
            <Canvas>
                <div className={styles.content}>
                    <Title>{t("trainingStatistics.title")}</Title>

                    <div className={styles.yearControls}>
                        <button
                            className={styles.yearButton}
                            type="button"
                            aria-label={t("trainingStatistics.previousYear")}
                            title={t("trainingStatistics.previousYear")}
                            disabled={!canMoveToPreviousYear}
                            onClick={() => moveToYear(previousYearWithStatistics)}
                        >
                            {"<"}
                        </button>

                        <h2 className={styles.yearTitle}>{selectedYear}</h2>

                        <button
                            className={styles.yearButton}
                            type="button"
                            aria-label={t("trainingStatistics.nextYear")}
                            title={t("trainingStatistics.nextYear")}
                            disabled={!canMoveToNextYear}
                            onClick={() => moveToYear(nextYearWithStatistics)}
                        >
                            {">"}
                        </button>
                    </div>

                    {isLoading ? (
                        <p className={styles.statusText}>{t("trainingStatistics.loading")}</p>
                    ) : error ? (
                        <p className={styles.error}>{error}</p>
                    ) : (
                        <>
                            <div className={styles.calendarWrap}>
                                <div className={styles.calendarGrid} aria-label={`${selectedYear} ${t("trainingStatistics.title")}`}>
                                    {yearDays.map(date => {
                                        const dateKey = getDateKey(date)
                                        const count = Number(statisticsByDate[dateKey] ?? 0)
                                        const level = getIntensityLevel(count, maxDailyCount)
                                        const formattedDate = dateFormatter.format(date)
                                        const tooltip = count > 0
                                            ? t("trainingStatistics.dayTooltip", { date: formattedDate, count })
                                            : t("trainingStatistics.emptyDayTooltip", { date: formattedDate })

                                        return (
                                            <div
                                                className={`${styles.dayCell} ${styles[`level${level}`]}`}
                                                key={dateKey}
                                                aria-label={tooltip}
                                                onMouseEnter={canUseDayTooltips ? event => showDayTooltipFromMouse(event, formattedDate, count) : undefined}
                                                onMouseMove={canUseDayTooltips ? moveDayTooltip : undefined}
                                                onMouseLeave={canUseDayTooltips ? hideDayTooltip : undefined}
                                                onFocus={canUseDayTooltips ? event => showDayTooltipFromFocus(event, formattedDate, count) : undefined}
                                                onBlur={canUseDayTooltips ? hideDayTooltip : undefined}
                                                tabIndex={canUseDayTooltips ? 0 : -1}
                                            />
                                        )
                                    })}
                                </div>
                            </div>

                            <section className={styles.summaryGrid} aria-label={t("trainingStatistics.summary")}>
                                <div className={styles.summaryItem}>
                                    <span>{t("trainingStatistics.totalNotes")}</span>
                                    <strong>{yearMetrics.totalNotes}</strong>
                                </div>

                                <div className={styles.summaryItem}>
                                    <span>{t("trainingStatistics.activeDays")}</span>
                                    <strong>{yearMetrics.activeDays}</strong>
                                </div>

                                <div className={styles.summaryItem}>
                                    <span>{t("trainingStatistics.completedDays")}</span>
                                    <strong>{yearMetrics.completionPercent}%</strong>
                                </div>

                                <div className={styles.summaryItem}>
                                    <span>{t("trainingStatistics.bestStreak")}</span>
                                    <strong>{yearMetrics.bestStreak}</strong>
                                </div>

                                <div className={styles.summaryItem}>
                                    <span>{t("trainingStatistics.currentStreak")}</span>
                                    <strong>{yearMetrics.currentStreak}</strong>
                                </div>

                                <div className={styles.summaryItem}>
                                    <span>{t("trainingStatistics.bestDay")}</span>
                                    <strong>
                                        {yearMetrics.bestDay && yearMetrics.bestDayCount > 0
                                            ? `${formatShortDate(yearMetrics.bestDay)} - ${yearMetrics.bestDayCount}`
                                            : "0"}
                                    </strong>
                                </div>

                                <div className={styles.summaryItem}>
                                    <span>{t("trainingStatistics.averageActiveDay")}</span>
                                    <strong>{numberFormatter.format(yearMetrics.averagePerActiveDay)}</strong>
                                </div>

                                <div className={styles.summaryItem}>
                                    <span>{t("trainingStatistics.averageCalendarDay")}</span>
                                    <strong>{numberFormatter.format(yearMetrics.averagePerCalendarDay)}</strong>
                                </div>
                            </section>

                            {canUseDayTooltips && hoveredDay && (
                                <div
                                    className={styles.dayTooltip}
                                    ref={tooltipRef}
                                    style={{
                                        left: hoveredDay.x,
                                        top: hoveredDay.y
                                    }}
                                >
                                    <strong>{hoveredDay.date}</strong>
                                    <span>{hoveredDay.notes}</span>
                                </div>
                            )}
                        </>
                    )}
                </div>
            </Canvas>
        </div>
    )
}

export default TrainingStatisticsCanvas
