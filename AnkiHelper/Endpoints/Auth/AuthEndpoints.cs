namespace AnkiHelper.Endpoints.Auth
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapUserLogin();
            app.MapUserRegister();
            app.MapUserMe();
            app.MapUserLogout();
            app.MapUserRefreshToken();
        }
    }
}
