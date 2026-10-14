namespace Application.UseCases.Auth
{
    public sealed record AuthTokensResponse(string AccessToken, string RefreshToken);
}
