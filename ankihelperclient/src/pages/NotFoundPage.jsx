
import { Navigate } from "react-router-dom"
import { isAuthenticated } from "../auth/authStorage"
import { useLanguage } from "../i18n/useLanguage"

const NotFoundPage = () => {
    const { t } = useLanguage()

    if (!isAuthenticated()) {
        return <Navigate to="/login" replace />
    }

    return (
        <div>
            <h1>{t("notFound.title")}</h1>
        </div>
    )


}

export default NotFoundPage
