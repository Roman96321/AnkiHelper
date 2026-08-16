using Application.UseCases.Decks.DeckImports.PrepareConfiguration;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AnkiHelper.Endpoints.ImportPipeline
{
    public static class PostDeckNamesEndpoint
    {
        public static void MapPostDeckNames(this IEndpointRouteBuilder app)
        {
            app.MapPost("/imports/preparation", async (
                [FromServices] PrepareDeckImportConfigurationUseCase source,
                [FromBody] List<string> deckNames,
                ClaimsPrincipal currentUser,
                CancellationToken token) =>
            {
                var userId = Guid.Parse(currentUser.FindFirstValue("userId")!);

                try
                {
                    var res = await source.PrepareConfigurationAsync(deckNames, userId, token);

                    return Results.Ok(res);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    return Results.StatusCode(499);
                }
            })
            .WithTags("Imports")
            .WithName("PrepareDeckImport")
            .RequireAuthorization();
        }
    }
}
