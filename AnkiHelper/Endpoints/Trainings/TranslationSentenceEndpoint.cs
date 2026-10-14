using Application.UseCases.Training.TranslationSentence;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AnkiHelper.Endpoints.Trainings
{
    public static class TranslationSentenceEndpoint
    {
        public static void MapTranslationSentence(this IEndpointRouteBuilder app)
        {
            app.MapPost("/training/translation-sentence/questions", async (
                [FromServices] TranslationSentenceUseCase source,
                [FromBody] TranslationSentenceRequest request,
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
            .WithName("GetTranslationSentenceQuestion")
            .RequireAuthorization();
        }
    }
}
