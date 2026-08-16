using Application.UseCases.Auth.RefreshTokens;
using Microsoft.AspNetCore.Mvc;

namespace AnkiHelper.Endpoints.Auth
{
    public static class UserRefreshToken
    {
        public static void MapUserRefreshToken(this IEndpointRouteBuilder app)
        {
            app.MapPost("/auth/refresh-token", async (
                [FromServices] RefreshTokenUseCase useCase,
                HttpContext httpContext,
                CancellationToken token) =>
            {
                try
                {
                    var refreshToken = httpContext.Request.Cookies[RefreshTokenCookie.Name] ?? string.Empty;
                    var request = new RefreshTokenRequest(refreshToken);
                    var response = await useCase.RefreshAsync(request, token);

                    RefreshTokenCookie.Append(httpContext.Response, response.RefreshToken);

                    return Results.Ok(new { response.AccessToken });
                }
                catch (UnauthorizedAccessException)
                {
                    RefreshTokenCookie.Delete(httpContext.Response);
                    return Results.Unauthorized();
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    return Results.StatusCode(499);
                }
            })
            .WithTags("Auth")
            .WithName("RefreshAccessToken");
        }
    }
}
