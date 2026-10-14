using Application.Services.ConfiguredDecks;
using Application.UseCases.Decks.Settings;
using Application.UseCases.Decks.Stats;
using Microsoft.EntityFrameworkCore;
using Application.Abstractions;
using Domain.Deck;

namespace Infrastructure.Persistence.Repositories;

internal class DeckRepository(AppDbContext dbContext) : IDeckRepository
{
    public async Task ReplaceAsync(List<Deck> decks, Guid userId, CancellationToken token)
    {
        var deckIds = decks
            .Select(deck => deck.Id)
            .Distinct()
            .ToList();

        var existingNotesById = await GetExistingNotesByIdAsync(token, userId, deckIds);
        var existingSettingsByDeckId = await GetExistingSettingsByDeckIdAsync(token, userId, deckIds);

        ApplyUserScope(decks, userId);
        PreserveExistingNoteProgress(decks, existingNotesById);
        PreserveExistingDeckSettings(decks, existingSettingsByDeckId);

        await ReplaceDecksAsync(decks, userId, token, deckIds);
    }

    public async Task ReplaceAllAsync(List<Deck> decks, Guid userId, CancellationToken token)
    {
        if (decks == null || decks.Count == 0)
            return;

        var existingNotesById = await GetExistingNotesByIdAsync(token, userId);
        var existingSettingsByDeckId = await GetExistingSettingsByDeckIdAsync(token, userId);

        ApplyUserScope(decks, userId);
        PreserveExistingNoteProgress(decks, existingNotesById);
        PreserveExistingDeckSettings(decks, existingSettingsByDeckId);

        await ReplaceDecksAsync(decks, userId, token);
    }

    private async Task<Dictionary<long, Note>> GetExistingNotesByIdAsync(CancellationToken token, Guid userId, List<long>? deckIds = null)
    {
        var query = dbContext.Decks
            .AsNoTracking()
            .Where(deck => deck.UserId == userId)
            .Include(deck => deck.Notes)
            .AsQueryable();

        if (deckIds is not null)
            query = query.Where(deck => deckIds.Contains(deck.Id));

        var existingDecks = await query.ToListAsync(token);

        return existingDecks
            .SelectMany(deck => deck.Notes)
            .ToDictionary(note => note.Id);
    }

    private async Task<Dictionary<long, ExistingDeckSettings>> GetExistingSettingsByDeckIdAsync(CancellationToken token, Guid userId, List<long>? deckIds = null)
    {
        var query = dbContext.Decks
            .AsNoTracking()
            .Where(deck => deck.UserId == userId)
            .AsQueryable();

        if (deckIds is not null)
            query = query.Where(deck => deckIds.Contains(deck.Id));

        return await query
            .Select(deck => new ExistingDeckSettings(
                deck.Id,
                deck.DeckDailyNewNotesCount,
                deck.UseOnlyLearnedNotes))
            .ToDictionaryAsync(settings => settings.DeckId, token);
    }

    private static void PreserveExistingNoteProgress(List<Deck> decks, Dictionary<long, Note> existingNotesById)
    {
        foreach (var note in decks.SelectMany(deck => deck.Notes))
        {
            if (!existingNotesById.TryGetValue(note.Id, out var existingNote))
                continue;

            note.LearningPoints = existingNote.LearningPoints;
            note.NextReviewDate = existingNote.NextReviewDate;
        }
    }

    private static void PreserveExistingDeckSettings(List<Deck> decks, Dictionary<long, ExistingDeckSettings> existingSettingsByDeckId)
    {
        foreach (var deck in decks)
        {
            if (!existingSettingsByDeckId.TryGetValue(deck.Id, out var existingSettings))
                continue;

            deck.DeckDailyNewNotesCount = existingSettings.DeckDailyNewNotesCount;
            deck.UseOnlyLearnedNotes = existingSettings.UseOnlyLearnedNotes;
        }
    }

    private static void ApplyUserScope(List<Deck> decks, Guid userId)
    {
        foreach (var deck in decks)
        {
            deck.UserId = userId;

            foreach (var note in deck.Notes)
            {
                note.UserId = userId;
                note.DeckId = deck.Id;
            }
        }
    }

    private async Task ReplaceDecksAsync(List<Deck> decks, Guid userId, CancellationToken token, List<long>? deckIds = null)
    {
        var replacingDeckIds = decks
            .Select(deck => deck.Id)
            .Distinct()
            .ToList();

        var existingDailyNewNoteProgress = await dbContext.DailyNewNoteProgress
            .AsNoTracking()
            .Where(progress => progress.UserId == userId && replacingDeckIds.Contains(progress.DeckId))
            .ToListAsync(token);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(token);

        if (deckIds is null)
        {
            await dbContext.Decks
                .Where(deck => deck.UserId == userId)
                .ExecuteDeleteAsync(token);
        }
        else
        {
            await dbContext.Decks
                .Where(deck => deckIds.Contains(deck.Id) && deck.UserId == userId)
                .ExecuteDeleteAsync(token);
        }

        await dbContext.Decks.AddRangeAsync(decks, token);
        await dbContext.DailyNewNoteProgress.AddRangeAsync(existingDailyNewNoteProgress, token);
        await dbContext.SaveChangesAsync(token);

        await transaction.CommitAsync(token);
    }

    public async Task<Dictionary<string, long>> GetAllDeckNamesAndIdsAsync(Guid userId, CancellationToken token)
    {
        return await dbContext.Decks
            .AsNoTracking()
            .Where(deck => deck.UserId == userId)
            .Select(deck => new { deck.Name, deck.Id })
            .ToDictionaryAsync(x => x.Name, x => x.Id, token);
    }

    public async Task SaveDeckImportSettingsAsync(List<DeckImportSelection> deckImportSelections, Guid userId, CancellationToken token)
    {
        if (deckImportSelections.Count == 0)
            return;

        var distinctDeckImportSelections = deckImportSelections
            .DistinctBy(deck => deck.DeckId)
            .ToList();

        var deckIds = distinctDeckImportSelections
            .Select(deck => deck.DeckId)
            .Distinct()
            .ToList();

        await dbContext.DeckImportSettings
            .Where(settings => deckIds.Contains(settings.DeckId) && settings.UserId == userId)
            .ExecuteDeleteAsync(token);

        var importSettings = new List<DeckImportSettings>();

        foreach (var deckImportSelection in distinctDeckImportSelections)
        {
            var fieldMappings = new List<DeckImportFieldMapping>();

            foreach (var fieldMapping in deckImportSelection.FieldMappings.DistinctBy(fieldMapping => new { fieldMapping.FrontKey, fieldMapping.BackKey }))
            {
                fieldMappings.Add(new DeckImportFieldMapping
                {
                    DeckId = deckImportSelection.DeckId,
                    UserId = userId,
                    FrontKey = fieldMapping.FrontKey,
                    BackKey = fieldMapping.BackKey
                });
            }

            importSettings.Add(new DeckImportSettings
            {
                DeckId = deckImportSelection.DeckId,
                UserId = userId,
                DeckName = deckImportSelection.DeckName,
                FieldMappings = fieldMappings
            });
        }

        dbContext.DeckImportSettings.AddRange(importSettings);

        await dbContext.SaveChangesAsync(token);
    }

    public async Task<List<DeckImportSettings>> GetDeckImportSettingsAsync(List<long> deckIds, Guid userId, CancellationToken token)
    {
        return await dbContext.DeckImportSettings
            .AsNoTracking()
            .Include(settings => settings.FieldMappings)
            .Where(settings => deckIds.Contains(settings.DeckId) && settings.UserId == userId)
            .ToListAsync(token);
    }

    public async Task<List<DeckStatsDto>> GetDecksStatsAsync(Guid userId, CancellationToken token)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var deckStats = await dbContext.Decks
            .AsNoTracking()
            .Where(deck => deck.UserId == userId)
            .Select(deck => new
            {
                DeckId = deck.Id,
                DeckName = deck.Name,
                DailyNewNoteCount = deck.DeckDailyNewNotesCount,
                LearnedNotes = deck.Notes.Where(note => !deck.UseOnlyLearnedNotes || note.IsLearned).Count(),
                AvailableNewNotes = deck.Notes.Where(note => note.NextReviewDate == DateOnly.MinValue && (!deck.UseOnlyLearnedNotes || note.IsLearned)).Count(),
                NewAnsweredToday = dbContext.DailyNewNoteProgress
                    .Where(progress => progress.UserId == userId && progress.DeckId == deck.Id && progress.Date == today)
                    .Select(progress => progress.NewAnsweredCount)
                    .FirstOrDefault(),
                WeakNotes = deck.Notes.Where(n => n.NextReviewDate != DateOnly.MinValue && n.NextReviewDate <= today && n.LearningPoints < 0).Count(),
                ReviewNotes = deck.Notes.Where(n => n.NextReviewDate != DateOnly.MinValue && n.NextReviewDate <= today && n.LearningPoints > 0).Count()
            })  
            .ToListAsync(token);

        return deckStats
            .Select(deck =>
            {
                var remainingNewNotesToday = Math.Max(0, deck.DailyNewNoteCount - deck.NewAnsweredToday);

                return new DeckStatsDto
                {
                    DeckId = deck.DeckId,
                    DeckName = deck.DeckName,
                    LearnedNotes = deck.LearnedNotes,
                    NewNotes = Math.Min(deck.AvailableNewNotes, remainingNewNotesToday),
                    WeakNotes = deck.WeakNotes,
                    ReviewNotes = deck.ReviewNotes
                };
            })
            .ToList();
    }

    public async Task<(string FrontLanguage, string BackLanguage)> GetDeckLanguage(long deckId, Guid userId, CancellationToken token)
    {
        var language = await dbContext.Decks
            .AsNoTracking()
            .Where(deck => deck.Id == deckId && deck.UserId == userId)
            .Select(deck => new
            {
                deck.FrontLanguage,
                deck.BackLanguage
            })
            .SingleOrDefaultAsync(token);

        if (language is null)
            throw new KeyNotFoundException($"Deck with id '{deckId}' was not found.");

        return (language.FrontLanguage, language.BackLanguage);
    }

    public async Task DeleteDecksAsync(long[] deckIds, Guid userId, CancellationToken token)
    {
        await dbContext.Decks.
            Where(deck => deckIds.Contains(deck.Id) && deck.UserId == userId).
            ExecuteDeleteAsync(token);

        await dbContext.DailyNewNoteProgress.
            Where(x => deckIds.Contains(x.DeckId) && x.UserId == userId).
            ExecuteDeleteAsync(token);

        await dbContext.DeckImportSettings.
            Where(x => deckIds.
            Contains(x.DeckId) && x.UserId == userId).
            ExecuteDeleteAsync(token);

        await dbContext.SaveChangesAsync(token);
    }

    public async Task<DeckSettings> GetSettingsStateAsync(Guid userId, CancellationToken token)
    {
        var settings = new DeckSettings();

        settings.NewDeckNames = await dbContext.Decks.
            AsNoTracking().
            Where(deck => deck.UserId == userId).
            ToDictionaryAsync(x => x.Id, x => x.Name, token);

        settings.DeckDailyNewNotesCount = await dbContext.Decks.
            AsNoTracking().
            Where(deck => deck.UserId == userId).
            ToDictionaryAsync(x => x.Id, x => x.DeckDailyNewNotesCount, token);

        settings.UseOnlyLearnedNotes = await dbContext.Decks.
            AsNoTracking().
            Where(deck => deck.UserId == userId).
            ToDictionaryAsync(x => x.Id, x => x.UseOnlyLearnedNotes, token);

        return settings;
    }

    public async Task UpdateSettingsAsync(DeckSettings settings, Guid userId, CancellationToken token)
    {
        if (settings is null)
            throw new ArgumentNullException(nameof(settings));

        var deckIds = settings.NewDeckNames.Keys
            .Concat(settings.DeckDailyNewNotesCount.Keys)
            .Concat(settings.UseOnlyLearnedNotes.Keys)
            .Distinct()
            .ToList();

        if (deckIds.Count == 0)
            return;

        var decks = await dbContext.Decks
            .Where(deck => deckIds.Contains(deck.Id) && deck.UserId == userId)
            .ToListAsync(token);

        foreach (var deck in decks)
        {
            if (settings.NewDeckNames.TryGetValue(deck.Id, out var deckName) &&
                !string.IsNullOrWhiteSpace(deckName))
                deck.Name = deckName;

            if (settings.DeckDailyNewNotesCount.TryGetValue(deck.Id, out var dailyNewNotesCount))
                deck.DeckDailyNewNotesCount = Math.Max(0, dailyNewNotesCount);

            if (settings.UseOnlyLearnedNotes.TryGetValue(deck.Id, out var useOnlyLearnedNotes))
                deck.UseOnlyLearnedNotes = useOnlyLearnedNotes;
        }

        await dbContext.SaveChangesAsync(token);
    }

    private sealed record ExistingDeckSettings(long DeckId, int DeckDailyNewNotesCount, bool UseOnlyLearnedNotes);
}
