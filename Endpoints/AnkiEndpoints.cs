using AnkiHelper.Endpoints.Home;
using AnkiHelper.Endpoints.ImportPipeline;

namespace AnkiHelper.Endpoints;

public static class AnkiEndpoints
{
    public static void MapAnkiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapCheckConnection();
        app.MapAnkiSource();
        app.MapAnkiApkgFile();
    }
}
