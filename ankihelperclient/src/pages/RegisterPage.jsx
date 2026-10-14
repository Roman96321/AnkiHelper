import { Link, useNavigate } from "react-router-dom"
import { useState } from "react"
import ankiHelperApi from "../api/ankiHelperApi"
import Button from "../Components/Button/Button"
import Canvas from "../Components/Canvas/Canvas"
import Title from "../Components/Title/Title"
import { useLanguage } from "../i18n/useLanguage"
import styles from "./AuthPage.module.css"

const RegisterPage = () => {
    const [email, setEmail] = useState("")
    const [password, setPassword] = useState("")
    const [confirmPassword, setConfirmPassword] = useState("")
    const [isPasswordVisible, setIsPasswordVisible] = useState(false)
    const [isConfirmPasswordVisible, setIsConfirmPasswordVisible] = useState(false)
    const [error, setError] = useState("")
    const [isSubmitting, setIsSubmitting] = useState(false)
    const { t } = useLanguage()
    const navigate = useNavigate()
    const isPasswordLongEnough = password.length >= 8
    const doPasswordsMatch = password === confirmPassword
    const canSubmit = isPasswordLongEnough && doPasswordsMatch && !isSubmitting

    const handleSubmit = async (event) => {
        event.preventDefault()
        setError("")

        if (!isPasswordLongEnough) {
            setError(t("auth.passwordTooShort"))
            return
        }

        if (password !== confirmPassword) {
            setError(t("auth.passwordsDoNotMatch"))
            return
        }

        setIsSubmitting(true)

        try {
            await ankiHelperApi.registerUser({ email, password })
            navigate("/login", {
                replace: true,
                state: { registered: true }
            })
        } catch (requestError) {
            setError(requestError.message || t("auth.registrationFailed"))
        } finally {
            setIsSubmitting(false)
        }
    }

    return (
        <Canvas>
            <div className={styles.content}>
                <Title>{t("auth.registerTitle")}</Title>

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
                                autoComplete="new-password"
                                value={password}
                                onChange={event => setPassword(event.target.value)}
                                minLength={8}
                                aria-describedby="password-requirements"
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
                        <span
                            className={`${styles.helperText} ${password && !isPasswordLongEnough ? styles.invalidHelperText : ""}`}
                            id="password-requirements"
                        >
                            {t("auth.passwordRequirement")}
                        </span>
                    </label>

                    <label className={styles.field}>
                        {t("auth.confirmPassword")}
                        <span className={styles.passwordInputWrap}>
                            <input
                                className={`${styles.input} ${styles.passwordInput}`}
                                type={isConfirmPasswordVisible ? "text" : "password"}
                                autoComplete="new-password"
                                value={confirmPassword}
                                onChange={event => setConfirmPassword(event.target.value)}
                                minLength={8}
                                required
                            />

                            <button
                                className={styles.passwordToggle}
                                type="button"
                                aria-label={isConfirmPasswordVisible ? t("auth.hidePassword") : t("auth.showPassword")}
                                title={isConfirmPasswordVisible ? t("auth.hidePassword") : t("auth.showPassword")}
                                onClick={() => setIsConfirmPasswordVisible(current => !current)}
                            >
                                {isConfirmPasswordVisible ? (
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
                        {confirmPassword && !doPasswordsMatch && (
                            <span className={`${styles.helperText} ${styles.invalidHelperText}`}>
                                {t("auth.passwordsDoNotMatch")}
                            </span>
                        )}
                    </label>

                    <div className={styles.actions}>
                        <Button type="submit" isDisabled={!canSubmit}>
                            {isSubmitting ? t("auth.creatingAccount") : t("auth.register")}
                        </Button>

                        <p className={styles.switchText}>
                            {t("auth.hasAccount")}{" "}
                            <Link className={styles.switchLink} to="/login">
                                {t("auth.login")}
                            </Link>
                        </p>
                    </div>
                </form>
            </div>
        </Canvas>
    )
}

export default RegisterPage
