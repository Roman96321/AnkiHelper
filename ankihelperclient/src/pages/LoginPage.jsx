import { Link, useLocation, useNavigate } from "react-router-dom"
import { useState } from "react"
import ankiHelperApi from "../api/ankiHelperApi"
import { saveTokens } from "../auth/authStorage"
import Button from "../Components/Button/Button"
import Canvas from "../Components/Canvas/Canvas"
import Title from "../Components/Title/Title"
import { useLanguage } from "../i18n/useLanguage"
import styles from "./AuthPage.module.css"

const LoginPage = () => {
    const [email, setEmail] = useState("")
    const [password, setPassword] = useState("")
    const [isPasswordVisible, setIsPasswordVisible] = useState(false)
    const [error, setError] = useState("")
    const [isSubmitting, setIsSubmitting] = useState(false)
    const { t } = useLanguage()
    const navigate = useNavigate()
    const location = useLocation()
    const redirectPath = location.state?.from?.pathname ?? "/"
    const wasRegistered = Boolean(location.state?.registered)

    const handleSubmit = async (event) => {
        event.preventDefault()
        setError("")
        setIsSubmitting(true)

        try {
            const tokens = await ankiHelperApi.loginUser({ email, password })
            saveTokens(tokens)
            navigate(redirectPath, { replace: true })
        } catch (requestError) {
            setError(requestError.message === "Unauthorized"
                ? t("auth.loginFailed")
                : requestError.message || t("auth.loginFailed"))
        } finally {
            setIsSubmitting(false)
        }
    }

    return (
        <Canvas>
            <div className={styles.content}>
                <Title>{t("auth.loginTitle")}</Title>

                {wasRegistered && (
                    <p className={styles.message}>{t("auth.registrationComplete")}</p>
                )}

                {error && <p className={styles.error}>{error}</p>}

                <form className={styles.form} onSubmit={handleSubmit}>
                    <label className={styles.field}>
                        {t("auth.email")}
                        <input
                            className={styles.input}
                            type="email"
                            autoComplete="email"
                            value={email}
                            onChange={event => setEmail(event.target.value)}
                            required
                        />
                    </label>

                    <label className={styles.field}>
                        {t("auth.password")}
                        <span className={styles.passwordInputWrap}>
                            <input
                                className={`${styles.input} ${styles.passwordInput}`}
                                type={isPasswordVisible ? "text" : "password"}
                                autoComplete="current-password"
                                value={password}
                                onChange={event => setPassword(event.target.value)}
                                required
                            />

                            <button
                                className={styles.passwordToggle}
                                type="button"
                                aria-label={isPasswordVisible ? t("auth.hidePassword") : t("auth.showPassword")}
                                title={isPasswordVisible ? t("auth.hidePassword") : t("auth.showPassword")}
                                onClick={() => setIsPasswordVisible(current => !current)}
                            >
                                {isPasswordVisible ? (
                                    <svg viewBox="0 0 24 24" aria-hidden="true">
                                        <path d="M3 3l18 18" />
                                        <path d="M10.6 10.6A2 2 0 0 0 12 14a2 2 0 0 0 1.4-.6" />
                                        <path d="M9.3 5.2A9.8 9.8 0 0 1 12 4c5 0 8.5 4.1 10 8a13.6 13.6 0 0 1-3.1 4.8" />
                                        <path d="M6.4 6.4A13.2 13.2 0 0 0 2 12c1.5 3.9 5 8 10 8a9.9 9.9 0 0 0 4.5-1.1" />
                                    </svg>
                                ) : (
                                    <svg viewBox="0 0 24 24" aria-hidden="true">
                                        <path d="M2 12s3.5-8 10-8 10 8 10 8-3.5 8-10 8S2 12 2 12z" />
                                        <circle cx="12" cy="12" r="3" />
                                    </svg>
                                )}
                            </button>
                        </span>
                    </label>

                    <div className={styles.actions}>
                        <Button type="submit" isDisabled={isSubmitting}>
                            {isSubmitting ? t("auth.loggingIn") : t("auth.login")}
                        </Button>

                        <p className={styles.switchText}>
                            {t("auth.noAccount")}{" "}
                            <Link className={styles.switchLink} to="/register">
                                {t("auth.createAccount")}
                            </Link>
                        </p>
                    </div>
                </form>
            </div>
        </Canvas>
    )
}

export default LoginPage
