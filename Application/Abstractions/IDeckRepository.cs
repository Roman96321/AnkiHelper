using Application.Services.ConfiguredDecks;
using Application.UseCases.Decks.Settings;
using Application.UseCases.Decks.Stats;
using Domain.Deck;

namespace Application.Abstractions;

public interface IDeckRepository
{
    Task ReplaceAsync(List<Deck> decks, Guid userId, CancellationToken token);
    Task ReplaceAllAsync(List<Deck> decks, Guid userId, CancellationToken token);
    Task SaveDeckImportSettingsAsync(List<DeckImportSelection> deckImportSelections, Guid userId, CancellationToken token);
    Task<Dictionary<string, long>> GetAllDeckNamesAndIdsAsync(Guid userId, CancellationToken token);
    Task<List<DeckImportSettings>> GetDeckImportSettingsAsync(List<long> deckIds, Guid userId, CancellationToken token);
    Task<List<DeckStatsDto>> GetDecksStatsAsync(Guid userId, CancellationToken token);
    Task DeleteDecksAsync(long[] deckIds,Guid userId, CancellationToken token);
    Task<(string FrontLanguage, string BackLanguage)> GetDeckLanguage(long deckId, Guid userId, CancellationToken token);
    Task<DeckSettings> GetSettingsStateAsync(Guid userId, CancellationToken token);
    Task UpdateSettingsAsync(DeckSettings settings, Guid userId, CancellationToken token);
}
