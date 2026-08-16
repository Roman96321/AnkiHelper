using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AnkiHelper.Endpoints.Home
{
    public static class AnkiSourceEndpoint
    {
        public static void MapAnkiSource(this IEndpointRouteBuilder app)
        {
            app.MapGet("/anki/source", ([FromServices] IConfiguration configuration) =>
            {
                return Results.Ok(configuration["AnkiDataSource"] ?? "Apkg");
            })
            .WithTags("Anki")
            .WithName("GetAnkiSource")
            .RequireAuthorization();
        }
    }
}
