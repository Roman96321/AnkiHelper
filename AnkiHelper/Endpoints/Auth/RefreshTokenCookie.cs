namespace AnkiHelper.Endpoints.Auth
{
    internal static class RefreshTokenCookie
    {
        public const string Name = "ankiHelperRefreshToken";

        private static CookieOptions CreateOptions() => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/api/auth",
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        };

        public static void Append(HttpResponse response, string refreshToken)
        {
            response.Cookies.Append(Name, refreshToken, CreateOptions());
        }

        public static void Delete(HttpResponse response)
        {
            response.Cookies.Delete(Name, new CookieOptions
            {
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/api/auth"
            });
        }
    }
}
