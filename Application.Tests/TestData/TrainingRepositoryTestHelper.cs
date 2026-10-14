using Application.Abstractions;
using Domain.Deck;
using NSubstitute;

namespace Application.Tests.TestData;

internal sealed class TrainingRepositoryTestHelper
{
    public ITrainingRepository Repository { get; } = Substitute.For<ITrainingRepository>();

    public static Note Note(
        long id,
        long deckId,
        Guid userId,
        string front = "dog",
        string back = "pes")
    {
        return new Note
        {
            Id = id,
            Front = front,
            Back = back,
            DeckId = deckId,
            UserId = userId,
            NextReviewDate = DateOnly.MinValue,
            IsLearned = true
        };
    }

    public void ReturnsNewNote(long deckId, Guid userId, Note? note)
    {
        Repository.GetNewNoteAsync(deckId, userId, Arg.Any<CancellationToken>())
            .Returns(note);
    }

    public void ReturnsReviewOrWeakNote(long deckId, Guid userId, Note? note)
    {
        Repository.GetReviewAndWeakNoteAsync(deckId, userId, Arg.Any<CancellationToken>())
            .Returns(note);
    }

    public void ReturnsRandomNotes(long deckId, long excludedNoteId, Guid userId, List<Note> notes)
    {
        Repository.GetRandomNotesAsync(
                deckId,
                excludedNoteId,
                Arg.Any<int>(),
                userId,
                Arg.Any<CancellationToken>())
            .Returns(notes);
    }

    public void ReturnsRandomNotes(
        long deckId,
        long excludedNoteId,
        int expectedCount,
        Guid userId,
        List<Note> notes)
    {
        Repository.GetRandomNotesAsync(
                deckId,
                excludedNoteId,
                expectedCount,
                userId,
                Arg.Any<CancellationToken>())
            .Returns(notes);
    }

    public void ReturnsRandomNotes(
        long deckId,
        long excludedNoteId,
        Func<int, bool> countMatcher,
        Guid userId,
        List<Note> notes)
    {
        Repository.GetRandomNotesAsync(
                deckId,
                excludedNoteId,
                Arg.Is<int>(count => countMatcher(count)),
                userId,
                Arg.Any<CancellationToken>())
            .Returns(notes);
    }

    public void ReturnsRemainingCount(long deckId, Guid userId, int count)
    {
        Repository.GetRemainingNotesCountAsync(deckId, userId, Arg.Any<CancellationToken>())
            .Returns(count);
    }
}
