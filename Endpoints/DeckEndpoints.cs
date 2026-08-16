using AnkiHelper.Endpoints.Home;
using AnkiHelper.Endpoints.ImportPipeline;
using AnkiHelper.Endpoints.Settings;

namespace AnkiHelper.Endpoints;

public static class DeckEndpoints
{
    public static void MapDeckEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGetDeckNames();
        app.MapPostDeckNames();
        app.MapImportDecks();
        app.MapPostSynchronizationDecks();
        app.MapGetDecksStats();
        app.MapDeleteDecks();
        app.MapGetSettingsState();
        app.MapUpdateSettings();
    }
}
