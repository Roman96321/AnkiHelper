using Application.UseCases.Decks.GetDeckNamesAndIds;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;

namespace AnkiHelper.Endpoints.ImportPipeline
{
    public static class GetDeckNamesEndpoint
    {
        public static void MapGetDeckNames(this IEndpointRouteBuilder app)
        {
            app.MapGet("/anki/decks", async ([FromServices] GetDeckNamesAndIdsUseCase source, CancellationToken token, ClaimsPrincipal currentUser) =>
            {
                var userId = Guid.Parse(currentUser.FindFirstValue("userId")!);

                try
                {
                    var res = await source.GetDeckNamesAndIdsAsync(token);
                    return Results.Ok(res);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    return Results.StatusCode(499);
                }
            })
            .WithTags("Anki")
            .WithName("GetAnkiDecks")
            .RequireAuthorization();
        }
    }
}
