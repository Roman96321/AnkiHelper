using Application.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;

namespace AnkiHelper.Endpoints.Home
{
    public static class DeleteDecksEndpoint
    {
        public static void MapDeleteDecks(this IEndpointRouteBuilder app)
        {
            app.MapDelete("/decks", async (
                [FromBody] long[] deckIds,
                [FromServices] IDeckRepository source,
                ClaimsPrincipal currentUser,
                CancellationToken token) =>
            {
                var userId = Guid.Parse(currentUser.FindFirstValue("userId")!);

                try
                {
                    await source.DeleteDecksAsync(deckIds, userId, token);
                    return Results.Ok();
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    return Results.StatusCode(499);
                }
            })
            .WithTags("Decks")
            .WithName("DeleteDecks")
            .RequireAuthorization();
        }
    }
}
