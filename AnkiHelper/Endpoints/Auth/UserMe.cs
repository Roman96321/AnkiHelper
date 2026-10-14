using System.Security.Claims;
using Application.Abstractions.Auth;
using Microsoft.AspNetCore.Mvc;

namespace AnkiHelper.Endpoints.Auth
{
    public sealed record UserMeResponse(string UserId, string Email);

    public static class UserMe
    {
        public static void MapUserMe(this IEndpointRouteBuilder app)
        {
            app.MapGet("/auth/me", async (
                [FromServices] IUserRepository userRepository,
                CancellationToken token,
                ClaimsPrincipal currentUser) =>
            {
                var userId = currentUser.FindFirstValue("userId");

                if (string.IsNullOrWhiteSpace(userId))
                    return Results.Unauthorized();

                var user = await userRepository.GetUserById(Guid.Parse(userId), token);

                return Results.Ok(new UserMeResponse(user.Id.ToString(), user.Email));
            })
            .WithTags("Auth")
            .WithName("GetCurrentUser")
            .RequireAuthorization();
        }
    }
}
