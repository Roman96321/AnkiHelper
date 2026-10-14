using Application.UseCases.Decks.DeckImports.ImportConfigured;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AnkiHelper.Endpoints.ImportPipeline
{
    public static class ImportDecksEndpoint
    {
        public static void MapImportDecks(this IEndpointRouteBuilder app)
        {
            app.MapPost("/imports", async (
                [FromBody] List<ConfiguredDeckImport> configuredDecks,
                [FromServices] ImportConfiguredDecksUseCase source,
                ClaimsPrincipal currentUser,
                CancellationToken token) =>
            {
                var userId = Guid.Parse(currentUser.FindFirstValue("userId")!);

                try 
                {
                    await source.ImportAsync(configuredDecks, userId, token);
                    return Results.Ok();
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    return Results.StatusCode(499);
                }
            })
            .WithTags("Imports")
            .WithName("ImportDecks")
            .RequireAuthorization();
        }
    }
}
