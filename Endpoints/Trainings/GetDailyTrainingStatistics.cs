using Application.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AnkiHelper.Endpoints.Trainings
{
    public static class GetDailyTrainingStatistics
    {
        public static void MapDailyTrainingStatistics(this IEndpointRouteBuilder app)
        {
            app.MapGet("/training/statistics", async (
                ClaimsPrincipal currentUser,
                CancellationToken token,
                [FromServices] ITrainingRepository repository) =>
            {
                var userId = Guid.Parse(currentUser.FindFirstValue("userId")!);

                try
                {
                    var res = await repository.GetDailyTrainingStatisticsAsync(userId, token);

                    return Results.Ok(res);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    return Results.StatusCode(499);
                }
            })
            .WithTags("Training")
            .WithName("GetTrainingStatistics")
            .RequireAuthorization();
        }
    }
}
