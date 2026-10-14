using Application.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AnkiHelper.Endpoints.Settings
{
    public static class GetSettingsStateEndpoint
    {
        public static void MapGetSettingsState(this IEndpointRouteBuilder app)
        {
            app.MapGet("/settings/decks", async (
                [FromServices] IDeckRepository source,
                ClaimsPrincipal currentUser,
                CancellationToken token) =>
            {
                var userId = Guid.Parse(currentUser.FindFirstValue("userId")!);

                try
                {
                    var res = await source.GetSettingsStateAsync(userId, token);

                    return Results.Ok(res);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    return Results.StatusCode(499);
                }
            })
            .WithTags("Settings")
            .WithName("GetDeckSettings")
            .RequireAuthorization();
        }
    }
}
