using System.Security.Claims;
using Application.Abstractions.Auth;
using Microsoft.AspNetCore.Mvc;

namespace AnkiHelper.Endpoints.Auth
{
    public static class UserLogout
    {
        public static void MapUserLogout(this IEndpointRouteBuilder app)
        {
            app.MapPost("/auth/logout", async (
                [FromServices] IUserRepository userRepository,
                [FromServices] IRefreshTokenProvider refreshTokenProvider,
                HttpContext httpContext,
                ClaimsPrincipal currentUser,
                CancellationToken token) =>
            {
                var rawRefreshToken = httpContext.Request.Cookies[RefreshTokenCookie.Name];

                if (string.IsNullOrWhiteSpace(rawRefreshToken))
                {
                    RefreshTokenCookie.Delete(httpContext.Response);
                    return Results.Ok();
                }

                var userId = currentUser.FindFirstValue("userId");

                if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var parsedUserId))
                    return Results.Unauthorized();

                var refreshTokenHash = refreshTokenProvider.Hash(rawRefreshToken);
                var refreshToken = await userRepository.GetRefreshTokenByHash(refreshTokenHash, token);

                if (refreshToken is null || refreshToken.UserId != parsedUserId)
                {
                    RefreshTokenCookie.Delete(httpContext.Response);
                    return Results.Unauthorized();
                }

                if (refreshToken.RevokedAtUtc is not null)
                {
                    RefreshTokenCookie.Delete(httpContext.Response);
                    return Results.Ok();
                }

                if (refreshToken.ExpiresAtUtc <= DateTime.UtcNow)
                {
                    RefreshTokenCookie.Delete(httpContext.Response);
                    return Results.Ok();
                }

                await userRepository.TryRevokeRefreshToken(refreshToken.Id, DateTime.UtcNow, token);
                RefreshTokenCookie.Delete(httpContext.Response);

                return Results.Ok();
            })
            .WithTags("Auth")
            .WithName("LogoutUser")
            .RequireAuthorization();
        }
    }
}
