using Application.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AnkiHelper.Endpoints.Home;

public static class GetDecksStatsEndpoint
{
    public static void MapGetDecksStats(this IEndpointRouteBuilder app)
    {
        app.MapGet("/decks/statistics", async ([FromServices] IDeckRepository repository, CancellationToken token, ClaimsPrincipal currentUser) =>
        {
            var userId = Guid.Parse(currentUser.FindFirstValue("userId")!);

            try
            {
                var result = await repository.GetDecksStatsAsync(userId, token);

                return Results.Ok(result);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                return Results.StatusCode(499);
            }
        })
        .WithTags("Decks")
        .WithName("GetDeckStatistics")
        .RequireAuthorization();
    }
}
