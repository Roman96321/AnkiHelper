import AddDecksPage from "./pages/AddDecksPage"
import HomePage from "./pages/HomePage"
import SelectTrainingPage from "./pages/SelectTrainingPage"
import ConfigureDecksPage from "./pages/ConfigureDecksPage"
import { Link, Routes, Route, useLocation} from 'react-router-dom'
import WordOrderPage from "./pages/WordOrderPage"
import SentenceCompositionPage from "./pages/SentenceCompositionPage"
import styles from "./App.module.css"
import NotFoundPage from "./pages/NotFoundPage.jsx"
import HelpPage from "./pages/HelpPage.jsx"
import SettingsPage from "./pages/SettingsPage.jsx"
import TranslationSentencePage from "./pages/TranslationSentencePage"
import { useLanguage } from "./i18n/useLanguage"
import TrainingStatisticsPage from "./pages/TrainingStatisticsPage.jsx"
import LoginPage from "./pages/LoginPage.jsx"
import RegisterPage from "./pages/RegisterPage.jsx"
import ProtectedRoute from "./auth/ProtectedRoute.jsx"
import { isAuthenticated } from "./auth/authStorage"

function App() {
    const location = useLocation()
    const { t } = useLanguage()
    const isHomePage = location.pathname === "/"
    const isAuthPage = location.pathname === "/login" || location.pathname === "/register"
    const canShowHeaderActions = isAuthenticated() && !isAuthPage

    return (
        <div className={styles.app}>
            <header className={styles.header}>
                <div>
                    <h1 className={styles.brand}>AnkiHelper</h1>
                    <p className={styles.tagline}>{t("app.tagline")}</p>
                </div>

                {canShowHeaderActions && (
                    <div className={styles.headerActions}>
                        {!isHomePage && (
                            <Link className={styles.homeLink} to="/">
                                {t("app.home")}
                            </Link>
                        )}
                    </div>
                )}
            </header>

            <Routes>
                <Route path="/login" element={<LoginPage />} />
                <Route path="/register" element={<RegisterPage />} />
                <Route element={<ProtectedRoute />}>
                    <Route path="/" element={<HomePage />} />
                    <Route path="/add-deck" element={<AddDecksPage />} />
                    <Route path="/configure-decks" element={<ConfigureDecksPage />} />
                    <Route path="/select-training" element={<SelectTrainingPage />} />
                    <Route path="/training/word-order" element={<WordOrderPage />} />
                    <Route path="/training/sentence-composition" element={<SentenceCompositionPage />} />
                    <Route path="/training/translation-sentence" element={<TranslationSentencePage />} />
                    <Route path="/help" element={<HelpPage />} />
                    <Route path="/settings" element={<SettingsPage />} />
                    <Route path="/training-statistics" element={<TrainingStatisticsPage />} />
                </Route>
                <Route path="*" element={<NotFoundPage />} />
            </Routes>
        </div>
    )
}

export default App
