using Application.UseCases.Training.WordOrder;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AnkiHelper.Endpoints.Trainings
{
    public static class WordOrderEndpoint
    {
        public static void MapWordOrder(this IEndpointRouteBuilder app)
        {
            app.MapPost("/training/word-order/questions", async (
                [FromServices] WordOrderUseCase source,
                [FromBody] WordOrderRequest request,
                ClaimsPrincipal currentUser,
                CancellationToken token) =>
            {
                var userId = Guid.Parse(currentUser.FindFirstValue("userId")!);

                try
                {
                    var res = await source.GetQuestionAsync(request, userId, token);
                    if (res is null)
                        return Results.NoContent();

                    return Results.Ok(res);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    return Results.StatusCode(499);
                }
            })
            .WithTags("Training")
            .WithName("GetWordOrderQuestion")
            .RequireAuthorization();
        }
    }
}
