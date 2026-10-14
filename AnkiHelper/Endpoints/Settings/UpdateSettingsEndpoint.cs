using Application.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using DeckSettings = Application.UseCases.Decks.Settings.DeckSettings;

namespace AnkiHelper.Endpoints.Settings
{
    public static class UpdateSettingsEndpoint
    {
        public static void MapUpdateSettings(this IEndpointRouteBuilder app)
        {
            app.MapPut("/settings/decks", async (
                [FromServices] IDeckRepository source,
                [FromBody] DeckSettings settings,
                ClaimsPrincipal currentUser,
                CancellationToken token) =>
            {
                var userId = Guid.Parse(currentUser.FindFirstValue("userId")!);

                try
                {
                    await source.UpdateSettingsAsync(settings, userId, token);

                    return Results.Ok();
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    return Results.StatusCode(499);
                }
            })
            .WithTags("Settings")
            .WithName("UpdateDeckSettings")
            .RequireAuthorization();
        }
    }
}
