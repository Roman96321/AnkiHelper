using Application.UseCases.Training.SentenceComposition;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AnkiHelper.Endpoints.Trainings
{
    public static class SentenceCompositionEndpoint
    {
        public static void MapSentenceComposition(this IEndpointRouteBuilder app)
        {
            app.MapPost("/training/sentence-composition/questions", async (
                [FromServices] SentenceCompositionUseCase source,
                [FromBody] SentenceCompositionRequest request,
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
            .WithName("GetSentenceCompositionQuestion")
            .RequireAuthorization();
        }
    }
}
