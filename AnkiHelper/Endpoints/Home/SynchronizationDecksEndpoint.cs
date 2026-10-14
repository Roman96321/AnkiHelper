using Application.UseCases.Decks.Synchronization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AnkiHelper.Endpoints.Home
{
    public static class SynchronizationDecksEndpoint
    {
        public static void MapPostSynchronizationDecks(this IEndpointRouteBuilder app)
        {
            app.MapPost("/decks/synchronization", async (
                [FromServices] DeckSynchronizationUseCase source,
                ClaimsPrincipal currentUser,
                CancellationToken token) =>
            {
                var userId = Guid.Parse(currentUser.FindFirstValue("userId")!);

                try
                {
                    await source.SynchronizeDecksAsync(userId, token);
                    return Results.Ok();
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    return Results.StatusCode(499);
                }
            })
            .WithTags("Decks")
            .WithName("SynchronizeDecks")
            .RequireAuthorization();
        }
    }
}
