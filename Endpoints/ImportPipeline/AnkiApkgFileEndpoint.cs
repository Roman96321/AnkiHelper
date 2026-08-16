using Infrastructure.AnkiPackage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace AnkiHelper.Endpoints.ImportPipeline;

public static class AnkiApkgFileEndpoint
{
    private const long MaxPackageSizeBytes = 100 * 1024 * 1024;

    public static void MapAnkiApkgFile(this IEndpointRouteBuilder app)
    {
        app.MapPost("/anki/packages", async (
            [FromForm] IFormFile file,
            [FromServices] IMemoryCache cache,
            ClaimsPrincipal currentUser,
            CancellationToken token) =>
        {
            var userId = Guid.Parse(currentUser.FindFirstValue("userId")!);

            if (file.Length == 0)
            { 
                return Results.BadRequest("APKG file is empty.");
            }

            if (file.Length > MaxPackageSizeBytes)
            {
                return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
            }

            if (!file.FileName.EndsWith(".apkg", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest("Only .apkg files are supported.");
            }

            await using var stream = file.OpenReadStream();
            using var memoryStream = new MemoryStream();

            await stream.CopyToAsync(memoryStream, token);

            cache.Set(
                AnkiPackageCacheKeys.GetAnkiPackageCacheKey(userId),
                memoryStream.ToArray(),
                TimeSpan.FromMinutes(30));

            return Results.Ok(new
            {
                fileName = file.FileName,
                size = file.Length
            });
        })
        .DisableAntiforgery()
        .WithMetadata(new RequestSizeLimitAttribute(MaxPackageSizeBytes))
        .WithTags("Anki")
        .WithName("UploadAnkiPackage")
        .RequireAuthorization();
    }
}
