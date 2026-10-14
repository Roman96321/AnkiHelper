import { useMemo, useState } from "react"
import {
    defaultLanguage,
    interfaceLanguageOptions,
    interfaceLanguageStorageKey,
    translations
} from "./translations"
import { LanguageContext } from "./languageContext"

const getNestedTranslation = (language, key) => {
    return key.split(".").reduce((currentValue, keyPart) => currentValue?.[keyPart], translations[language])
}

const formatTranslation = (text, values = {}) => {
    return Object.entries(values).reduce(
        (result, [key, value]) => result.replaceAll(`{${key}}`, value),
        text
    )
}

const getInitialLanguage = () => {
    const savedLanguage = localStorage.getItem(interfaceLanguageStorageKey)

    return interfaceLanguageOptions.some(language => language.value === savedLanguage)
        ? savedLanguage
        : defaultLanguage
}

export const LanguageProvider = ({ children }) => {
    const [language, setLanguageState] = useState(getInitialLanguage)

    const setLanguage = (newLanguage) => {
        const nextLanguage = interfaceLanguageOptions.some(option => option.value === newLanguage)
            ? newLanguage
            : defaultLanguage

        localStorage.setItem(interfaceLanguageStorageKey, nextLanguage)
        setLanguageState(nextLanguage)
    }

    const value = useMemo(() => {
        const t = (key, values) => {
            const translation = getNestedTranslation(language, key)
                ?? getNestedTranslation(defaultLanguage, key)
                ?? key

            return formatTranslation(translation, values)
        }

        return {
            language,
            setLanguage,
            t
        }
    }, [language])

    return (
        <LanguageContext.Provider value={value}>
            {children}
        </LanguageContext.Provider>
    )
}
