import { useEffect, useRef, useState } from "react"
import Canvas from "../../Components/Canvas/Canvas"
import Title from "../../Components/Title/Title"
import styles from "./HelpPageCanvas.module.css"
import { useLanguage } from "../../i18n/useLanguage"

const sections = [
    { id: "import-pc", titleKey: "help.importPc" },
    { id: "import-mobile", titleKey: "help.importMobile" },
    { id: "questions", titleKey: "help.questions" }
]

const imageExpandDelayMs = 180

const HelpPageCanvas = () => {
    const [activeSectionId, setActiveSectionId] = useState(sections[0].id)
    const [expandedImage, setExpandedImage] = useState(null)
    const [isScrollTopButtonVisible, setIsScrollTopButtonVisible] = useState(false)
    const { t } = useLanguage()
    const sectionRefs = useRef({})
    const imageExpandTimerRef = useRef(null)

    const cancelScheduledImageExpand = () => {
        if (!imageExpandTimerRef.current) {
            return
        }

        clearTimeout(imageExpandTimerRef.current)
        imageExpandTimerRef.current = null
    }

    const scheduleExpandedImage = (image) => {
        cancelScheduledImageExpand()

        imageExpandTimerRef.current = setTimeout(() => {
            setExpandedImage(image)
            imageExpandTimerRef.current = null
        }, imageExpandDelayMs)
    }

    const createImageButtonProps = (image) => {
        return {
            onPointerDown: () => scheduleExpandedImage(image),
            onPointerUp: () => cancelScheduledImageExpand(),
            onPointerCancel: () => cancelScheduledImageExpand(),
            onPointerLeave: () => cancelScheduledImageExpand(),
            onPointerMove: () => cancelScheduledImageExpand(),
            onContextMenu: event => event.preventDefault()
        }
    }

    useEffect(() => {
        const observer = new IntersectionObserver(
            entries => {
                const visibleEntry = entries
                    .filter(entry => entry.isIntersecting)
                    .sort((firstEntry, secondEntry) => secondEntry.intersectionRatio - firstEntry.intersectionRatio)[0]

                if (visibleEntry?.target.id) {
                    setActiveSectionId(visibleEntry.target.id)
                }
            },
            {
                root: null,
                rootMargin: "-20% 0px -45% 0px",
                threshold: [0.2, 0.45, 0.7]
            }
        )

        Object.values(sectionRefs.current).forEach(section => {
            if (section) {
                observer.observe(section)
            }
        })

        return () => observer.disconnect()
    }, [])

    useEffect(() => {
        if (!expandedImage) {
            return
        }

        const closeExpandedImage = () => {
            setExpandedImage(null)
        }

        window.addEventListener("pointerup", closeExpandedImage)
        window.addEventListener("pointercancel", closeExpandedImage)

        return () => {
            window.removeEventListener("pointerup", closeExpandedImage)
            window.removeEventListener("pointercancel", closeExpandedImage)
        }
    }, [expandedImage])

    useEffect(() => {
        return () => cancelScheduledImageExpand()
    }, [])

    useEffect(() => {
        const updateScrollTopButtonVisibility = () => {
            setIsScrollTopButtonVisible(window.scrollY > 92)
        }

        updateScrollTopButtonVisibility()
        window.addEventListener("scroll", updateScrollTopButtonVisibility, { passive: true })

        return () => {
            window.removeEventListener("scroll", updateScrollTopButtonVisibility)
        }
    }, [])

    const scrollToSection = (sectionId) => {
        sectionRefs.current[sectionId]?.scrollIntoView({
            behavior: "smooth",
            block: "start"
        })
    }

    const scrollToTop = () => {
        window.scrollTo({
            top: 0,
            behavior: "smooth"
        })
    }

    return (
        <div>
            <Canvas>
                <div className={styles.layout}>
                    <main className={styles.content}>
                        <Title>{t("help.title")}</Title>

                        <section
                            className={styles.section}
                            id="import-pc"
                            ref={section => { sectionRefs.current["import-pc"] = section }}
                        >
                            <h2 className={styles.sectionTitle}>{t("help.importPc")}</h2>
                            <p className={styles.stepLine}>{t("help.pcStep")}</p>

                            <div className={styles.imageStack}>
                                <button
                                    className={styles.imageButton}
                                    type="button"
                                    {...createImageButtonProps({ src: "/help-images/Guide1.png", alt: "Anki file menu export step" })}
                                >
                                    <img className={styles.guideImage} src="/help-images/Guide1.png" alt="Anki file menu export step" width="1275" height="702" draggable="false" />
                                </button>
                                <button
                                    className={styles.imageButton}
                                    type="button"
                                    {...createImageButtonProps({ src: "/help-images/Guide2.png", alt: "Anki export dialog step" })}
                                >
                                    <img className={styles.guideImage} src="/help-images/Guide2.png" alt="Anki export dialog step" width="1420" height="782" draggable="false" />
                                </button>
                                <button
                                    className={styles.imageButton}
                                    type="button"
                                    {...createImageButtonProps({ src: "/help-images/Guide3.png", alt: "Anki package export settings step" })}
                                >
                                    <img className={styles.guideImage} src="/help-images/Guide3.png" alt="Anki package export settings step" width="1422" height="783" draggable="false" />
                                </button>
                            </div>

                            <div className={styles.settingsList}>
                                <div className={styles.settingRow}>
                                    <span>{t("help.exportFormat")}</span>
                                    <strong>.apkg</strong>
                                </div>
                                <div className={styles.settingRow}>
                                    <span>{t("help.include")}</span>
                                    <strong>{t("help.allDecks")}</strong>
                                </div>
                                <div className={styles.settingRow}>
                                    <span>{t("help.includeScheduling")}</span>
                                    <strong>{t("help.on")}</strong>
                                </div>
                                <div className={styles.settingRow}>
                                    <span>{t("help.includeDeckPresets")}</span>
                                    <strong>{t("help.off")}</strong>
                                </div>
                                <div className={styles.settingRow}>
                                    <span>{t("help.includeMedia")}</span>
                                    <strong>{t("help.off")}</strong>
                                </div>
                                <div className={styles.settingRow}>
                                    <span>{t("help.supportOlderVersions")}</span>
                                    <strong>{t("help.off")}</strong>
                                </div>
                            </div>

                            <p className={styles.stepLine}>{t("help.pcFinish")}</p>
                        </section>

                        <section
                            className={styles.section}
                            id="import-mobile"
                            ref={section => { sectionRefs.current["import-mobile"] = section }}
                        >
                            <h2 className={styles.sectionTitle}>{t("help.importMobile")}</h2>
                            <p className={styles.stepLine}>{t("help.mobileStep")}</p>

                            <div className={styles.imageStack}>
                                <button
                                    className={styles.imageButton}
                                    type="button"
                                    {...createImageButtonProps({ src: "/help-images/GuideMobile1.png", alt: "AnkiDroid three-dot menu step" })}
                                >
                                    <img className={styles.guideImage} src="/help-images/GuideMobile1.png" alt="AnkiDroid three-dot menu step" width="392" height="881" draggable="false" />
                                </button>
                                <button
                                    className={styles.imageButton}
                                    type="button"
                                    {...createImageButtonProps({ src: "/help-images/GuideMobile2.png", alt: "AnkiDroid export menu step" })}
                                >
                                    <img className={styles.guideImage} src="/help-images/GuideMobile2.png" alt="AnkiDroid export menu step" width="380" height="854" draggable="false" />
                                </button>
                                <button
                                    className={styles.imageButton}
                                    type="button"
                                    {...createImageButtonProps({ src: "/help-images/GuideMobile3.png", alt: "AnkiDroid package export settings step" })}
                                >
                                    <img className={styles.guideImage} src="/help-images/GuideMobile3.png" alt="AnkiDroid package export settings step" width="387" height="877" draggable="false" />
                                </button>
                            </div>

                            <div className={styles.settingsList}>
                                <div className={styles.settingRow}>
                                    <span>{t("help.exportFormat")}</span>
                                    <strong>.apkg</strong>
                                </div>
                                <div className={styles.settingRow}>
                                    <span>{t("help.include")}</span>
                                    <strong>{t("help.allDecks")}</strong>
                                </div>
                                <div className={styles.settingRow}>
                                    <span>{t("help.includeScheduling")}</span>
                                    <strong>{t("help.on")}</strong>
                                </div>
                                <div className={styles.settingRow}>
                                    <span>{t("help.includeDeckPresets")}</span>
                                    <strong>{t("help.off")}</strong>
                                </div>
                                <div className={styles.settingRow}>
                                    <span>{t("help.includeMedia")}</span>
                                    <strong>{t("help.off")}</strong>
                                </div>
                                <div className={styles.settingRow}>
                                    <span>{t("help.supportOlderVersions")}</span>
                                    <strong>{t("help.off")}</strong>
                                </div>
                            </div>

                            <p className={styles.stepLine}>{t("help.mobileFinish")}</p>
                        </section>

                        <section
                            className={styles.section}
                            id="questions"
                            ref={section => { sectionRefs.current.questions = section }}
                        >
                            <h2 className={styles.sectionTitle}>{t("help.questions")}</h2>

                            <div className={styles.questionBlock}>
                                <h3>{t("help.whatForTitle")}</h3>
                                <p>{t("help.whatForText")}</p>
                            </div>

                            <div className={styles.questionBlock}>
                                <h3>{t("help.learnedOnlyTitle")}</h3>
                                <p>{t("help.learnedOnlyText")}</p>
                            </div>

                            <div className={styles.questionBlock}>
                                <h3>{t("help.schedulingTitle")}</h3>
                                <p>{t("help.schedulingText")}</p>
                            </div>

                            <div className={styles.questionBlock}>
                                <h3>{t("help.mediaTitle")}</h3>
                                <p>{t("help.mediaText")}</p>
                            </div>
                        </section>
                    </main>

                    <aside className={styles.navigation} aria-label={t("help.navAria")}>
                        {sections.map(section => (
                            <button
                                className={`${styles.navButton} ${activeSectionId === section.id ? styles.navButtonActive : ""}`}
                                key={section.id}
                                type="button"
                                onClick={() => scrollToSection(section.id)}
                            >
                                {t(section.titleKey)}
                            </button>
                        ))}
                    </aside>
                </div>

                {expandedImage && (
                    <div className={styles.imageOverlay}>
                        <img className={styles.expandedImage} src={expandedImage.src} alt={expandedImage.alt} draggable="false" />
                    </div>
                )}

                {isScrollTopButtonVisible && (
                    <button
                        className={styles.scrollTopButton}
                        type="button"
                        aria-label={t("help.backToTop")}
                        title={t("help.backToTop")}
                        onClick={scrollToTop}
                    >
                        {"↑"}
                    </button>
                )}
            </Canvas>
        </div>
    )
}

export default HelpPageCanvas
