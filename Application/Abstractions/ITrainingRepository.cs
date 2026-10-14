using Domain.Deck;
using System;

namespace Application.Abstractions
{
    public interface ITrainingRepository
    {
        Task<Note?> GetNewNoteAsync(long deckId, Guid userId, CancellationToken token);
        Task<Note?> GetReviewAndWeakNoteAsync(long deckId, Guid userId, CancellationToken token);
        Task<List<Note>> GetRandomNotesAsync(long deckId, long excludedNoteId, int count, Guid userId, CancellationToken token);
        Task UpdateNote(long deckId, long noteId, bool isCorrectAnswer, Guid userId, CancellationToken token);
        Task<int> GetRemainingNotesCountAsync(long deckId, Guid userId, CancellationToken token);
        Task<Dictionary<DateOnly, int>> GetDailyTrainingStatisticsAsync(Guid userId, CancellationToken token);
    }
}
