using Application.UseCases.Anki.CheckConnection;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;

namespace AnkiHelper.Endpoints.Home
{
    public static class CheckAnkiConnectionEndpoint
    {
        public static void MapCheckConnection(this IEndpointRouteBuilder app)
        {
            app.MapGet("/anki/connection", async ([FromServices] CheckAnkiConnectionUseCase source, CancellationToken token) =>
            {
                try
                {
                    var res = await source.CheckConnectionAsync(token);
                    return Results.Ok(res);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    return Results.StatusCode(499);
                }
            })
            .WithTags("Anki")
            .WithName("CheckAnkiConnection")
            .RequireAuthorization();
        }
    }
}
