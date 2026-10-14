using Microsoft.EntityFrameworkCore;
using Application.Abstractions;
using Domain.Deck;
using System;

namespace Infrastructure.Persistence.Repositories
{
    public class TrainingRepository(AppDbContext dbContext) : ITrainingRepository
    {
        public async Task<Note?> GetNewNoteAsync(long deckId, Guid userId, CancellationToken token)
        {
            var useOnlyLearnedNote = await dbContext.Decks.
                AsNoTracking().
                Where(x => x.Id == deckId && x.UserId == userId).
                Select(deck => deck.UseOnlyLearnedNotes).
                FirstAsync(token);

            var dailyNewNotesLimit = await dbContext.Decks.
                AsNoTracking().
                Where(x => x.Id == deckId && x.UserId == userId).
                Select(deck => deck.DeckDailyNewNotesCount).
                FirstAsync(token);

            var today = DateOnly.FromDateTime(DateTime.Today);
            var newAnsweredToday = await dbContext.DailyNewNoteProgress
                .AsNoTracking()
                .Where(progress => progress.DeckId == deckId && progress.Date == today && progress.UserId == userId)
                .Select(progress => progress.NewAnsweredCount)
                .FirstOrDefaultAsync(token);

            if (newAnsweredToday >= dailyNewNotesLimit)
                return null;

            return await dbContext.Decks.
                AsNoTracking().
                Where(deck => deck.Id == deckId && deck.UserId == userId).
                SelectMany(deck => deck.Notes).
                Where(note => note.NextReviewDate == DateOnly.MinValue).
                Where(note => !useOnlyLearnedNote || note.IsLearned).
                FirstOrDefaultAsync(token);
        }

        public async Task<List<Note>> GetRandomNotesAsync(long deckId, long excludedNoteId, int count, Guid userId, CancellationToken token)
        {
            return await dbContext.Decks.
                AsNoTracking().
                Where(deck => deck.Id == deckId && deck.UserId == userId).
                SelectMany(deck => deck.Notes).
                Where(note => note.IsLearned == true && note.Id != excludedNoteId).
                OrderBy(x => EF.Functions.Random()).
                Take(count).
                ToListAsync(token);
        }

        public async Task<Note?> GetReviewAndWeakNoteAsync(long deckId, Guid userId, CancellationToken token)
        {
            return await dbContext.Decks.
                AsNoTracking().
                Where(deck => deck.Id == deckId && deck.UserId == userId).
                SelectMany(deck => deck.Notes).
                Where(note => note.NextReviewDate != DateOnly.MinValue && note.NextReviewDate <= DateOnly.FromDateTime(DateTime.Today)).
                OrderBy(x => EF.Functions.Random()).
                FirstOrDefaultAsync(token);
        }

        public async Task UpdateNote(long deckId, long noteId, bool isCorrectAnswer, Guid userId, CancellationToken token)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            var note = await dbContext.Decks.
                Where(deck => deck.Id == deckId && deck.UserId == userId).
                SelectMany(deck => deck.Notes).
                Where(note => note.Id == noteId).
                FirstAsync(token);

            if (note.NextReviewDate == DateOnly.MinValue)
            {
                var newNoteProgress = await dbContext.DailyNewNoteProgress.
                    Where(x => x.DeckId == deckId && x.Date == today && x.UserId == userId).
                    FirstOrDefaultAsync(token);

                if (newNoteProgress == null)
                {
                    await dbContext.DailyNewNoteProgress.
                        AddAsync(new DailyNewNoteProgress
                        {
                            UserId = userId,
                            DeckId = deckId,
                            Date = today,
                            NewAnsweredCount = 1
                        }, token);
                }
                else 
                    newNoteProgress.NewAnsweredCount++;
            }

            if (isCorrectAnswer) 
            {
                if (note.LearningPoints < 0)
                    note.LearningPoints = 0;

                note.LearningPoints++;

                var dailyTrainingStats = await dbContext.DailyTrainingStatistics.
                    Where(x => x.Date == today && x.UserId == userId).
                    FirstOrDefaultAsync(token);

                if (dailyTrainingStats == null)
                {
                    await dbContext.DailyTrainingStatistics.
                        AddAsync(new DailyTrainingStatistics
                        {
                            UserId = userId,
                            Date = today,
                            LearningNoteCounts = 1
                        }, token);
                }
                else
                {
                    dailyTrainingStats.LearningNoteCounts++;
                }
            }
            else
            {
                if (note.LearningPoints >= 4)
                    note.LearningPoints /= 2;
                else
                    note.LearningPoints--;

                if (note.LearningPoints <= -4)
                    note.LearningPoints = -1;
            }

            var nextReview = DateOnly.FromDateTime(DateTime.Today);

            int days = note.LearningPoints switch
            {
                <= -3 => 1,
                -2 => 0,
                -1 => 0,
                0 => 0,
                1 => 2,
                2 => 3,
                3 => 7,
                4 => 14,
                5 => 21,
                6 => 28,
                >= 7 => 60
            };

            note.NextReviewDate = nextReview.AddDays(days);

            await dbContext.SaveChangesAsync(token);
        }

        public async Task<int> GetRemainingNotesCountAsync(long deckId, Guid userId, CancellationToken token)
        {
            var dailyNewNotesLimit = await dbContext.Decks.
                AsNoTracking().
                Where(x => x.Id == deckId && x.UserId == userId).
                Select(deck => deck.DeckDailyNewNotesCount).
                FirstAsync(token);

            var useOnlyLearnedNote = await dbContext.Decks.
                AsNoTracking().
                Where(x => x.Id == deckId && x.UserId == userId).
                Select(deck => deck.UseOnlyLearnedNotes).
                FirstAsync(token);

            var today = DateOnly.FromDateTime(DateTime.Today);
            var newAnsweredToday = await dbContext.DailyNewNoteProgress
                .AsNoTracking()
                .Where(progress => progress.DeckId == deckId && progress.Date == today && progress.UserId == userId)
                .Select(progress => progress.NewAnsweredCount)
                .FirstOrDefaultAsync(token);

            var remainingNewNotesToday = Math.Max(0, dailyNewNotesLimit - newAnsweredToday);

            var newCount = await dbContext.Decks.
                AsNoTracking().
                Where(deck => deck.Id == deckId && deck.UserId == userId).
                SelectMany(deck => deck.Notes).
                Where(note => note.NextReviewDate == DateOnly.MinValue).
                Where(note => !useOnlyLearnedNote || note.IsLearned).
                Take(remainingNewNotesToday).
                CountAsync(token);

            var reviewCount = await dbContext.Decks.
                AsNoTracking().
                Where(deck => deck.Id == deckId && deck.UserId == userId).
                SelectMany(deck => deck.Notes).
                Where(note => note.NextReviewDate != DateOnly.MinValue && note.NextReviewDate <= DateOnly.FromDateTime(DateTime.Today)).
                CountAsync(token);

            return newCount + reviewCount;               
        }

        public async Task<Dictionary<DateOnly, int>> GetDailyTrainingStatisticsAsync(Guid userId, CancellationToken token)
        {
            return await dbContext.DailyTrainingStatistics
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .ToDictionaryAsync(x => x.Date, x => x.LearningNoteCounts, token);
        }
    }
}
