const accessTokenKey = "ankiHelperAccessToken"
const refreshTokenKey = "ankiHelperRefreshToken"

export const saveTokens = (tokens) => {
    const accessToken = tokens.accessToken ?? tokens.AccessToken

    if (accessToken) {
        localStorage.setItem(accessTokenKey, accessToken)
    }
}

export const getAccessToken = () => localStorage.getItem(accessTokenKey)

export const clearTokens = () => {
    localStorage.removeItem(accessTokenKey)
    localStorage.removeItem(refreshTokenKey)
}

export const isAuthenticated = () => Boolean(getAccessToken())
