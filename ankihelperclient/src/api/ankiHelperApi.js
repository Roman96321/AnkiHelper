import { clearTokens, getAccessToken, saveTokens } from "../auth/authStorage"

const URL = 'https://localhost:7041'
const headers = {
    'Content-Type': 'application/json'
}
let refreshTokenRequest = null

const getAuthHeaders = () => {
    const accessToken = getAccessToken()

    return accessToken
        ? { Authorization: `Bearer ${accessToken}` }
        : {}
}

const getJsonHeaders = () => ({
    ...headers,
    ...getAuthHeaders()
})

const redirectToLogin = () => {
    if (window.location.pathname !== "/login") {
        window.location.assign("/login")
    }
}

const readResponse = async (response) => {
    const text = await response.text()

    if (response.status === 401) {
        clearTokens()
        redirectToLogin()
        throw new Error("Unauthorized")
    }

    if (!response.ok) {
        let errorMessage = text

        try {
            const problem = text ? JSON.parse(text) : null
            errorMessage = problem?.title || problem?.detail || errorMessage
        } catch {
            errorMessage = text
        }

        throw new Error(errorMessage || `Request failed with status ${response.status}`)
    }

    return text ? JSON.parse(text) : null
}

const refreshTokens = async () => {
    if (refreshTokenRequest) {
        return await refreshTokenRequest
    }

    refreshTokenRequest = (async () => {
        const response = await fetch(`${URL}/api/auth/refresh-token`, {
            method: 'POST',
            credentials: 'include'
        })

        if (!response.ok) {
            return false
        }

        const tokens = await readResponse(response)
        saveTokens(tokens)

        return true
    })()

    try {
        return await refreshTokenRequest
    } finally {
        refreshTokenRequest = null
    }
}

const fetchWithAuthRetry = async (path, options = {}) => {
    const requestOptions = {
        ...options,
        credentials: 'include',
        headers: {
            ...(options.headers ?? {}),
            ...getAuthHeaders()
        }
    }

    const response = await fetch(`${URL}${path}`, requestOptions)

    if (response.status !== 401) {
        return await readResponse(response)
    }

    const wasRefreshed = await refreshTokens()

    if (!wasRefreshed) {
        clearTokens()
        redirectToLogin()
        throw new Error("Unauthorized")
    }

    const retryResponse = await fetch(`${URL}${path}`, {
        ...options,
        credentials: 'include',
        headers: {
            ...(options.headers ?? {}),
            ...getAuthHeaders()
        }
    })

    return await readResponse(retryResponse)
}

const ankiHelperApi = {
    registerUser: async (request) => {
        const response = await fetch(`${URL}/api/auth/register`, {
            method: 'POST',
            headers,
            body: JSON.stringify(request)
        })

        return await readResponse(response)
    },

    loginUser: async (request) => {
        const response = await fetch(`${URL}/api/auth/login`, {
            method: 'POST',
            headers,
            credentials: 'include',
            body: JSON.stringify(request)
        })

        return await readResponse(response)
    },

    logoutUser: async () => {
        const response = await fetch(`${URL}/api/auth/logout`, {
            method: 'POST',
            headers: getJsonHeaders(),
            credentials: 'include'
        })

        return await readResponse(response)
    },

    getCurrentUser: async () => {
        return await fetchWithAuthRetry('/api/auth/me')
    },

    checkConnection: async () => {
        return await fetchWithAuthRetry('/api/anki/connection')
    },

    getDecksName: async () => {
        return await fetchWithAuthRetry('/api/anki/decks')
    },

    getAnkiSource: async () => {
        return await fetchWithAuthRetry('/api/anki/source')
    },

    uploadAnkiApkgFile: async (file) => {
        const formData = new FormData()
        formData.append('file', file)

        return await fetchWithAuthRetry('/api/anki/packages', {
            method: 'POST',
            body: formData
        })
    },

    postDecksName: async (deckNames) => {
        return await fetchWithAuthRetry('/api/imports/preparation', {
            method: 'POST',
            headers: getJsonHeaders(),
            body: JSON.stringify(deckNames)
        })
    },

    deleteDecks: async (deckIds) => {
        return await fetchWithAuthRetry('/api/decks', {
            method: 'DELETE',
            headers: getJsonHeaders(),
            body: JSON.stringify(deckIds)
        })
    },

    postImportConfiguration: async (configuration) => {
        return await fetchWithAuthRetry('/api/imports', {
            method: 'POST',
            headers: getJsonHeaders(),
            body: JSON.stringify(configuration)
        })
    },

    postSynchronization: async () => {
        return await fetchWithAuthRetry('/api/decks/synchronization', {
            method: 'POST',
            headers: getJsonHeaders()
        })
    },

    getDecksStats: async () => {
        return await fetchWithAuthRetry('/api/decks/statistics')
    },

    getTrainingStatistics: async () => {
        return await fetchWithAuthRetry('/api/training/statistics')
    },

    getSettingsState: async () => {
        return await fetchWithAuthRetry('/api/settings/decks')
    },

    updateSettings: async (settings) => {
        return await fetchWithAuthRetry('/api/settings/decks', {
            method: 'PUT',
            headers: getJsonHeaders(),
            body: JSON.stringify(settings)
        })
    },

    postTrainingWordOrder: async (answer) => {
        return await fetchWithAuthRetry('/api/training/word-order/questions', {
            method: 'POST',
            headers: getJsonHeaders(),
            body: JSON.stringify(answer)
        })
    },

    postSentenceComposition: async (answer) => {
        return await fetchWithAuthRetry('/api/training/sentence-composition/questions', {
            method: 'POST',
            headers: getJsonHeaders(),
            body: JSON.stringify(answer)
        })
    },

    postTranslationSentence: async (answer) => {
        return await fetchWithAuthRetry('/api/training/translation-sentence/questions', {
            method: 'POST',
            headers: getJsonHeaders(),
            body: JSON.stringify(answer)
        })
    }
}

export default ankiHelperApi
