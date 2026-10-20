using Domain.Deck;
using Domain.Users;

namespace Infrastructure.Tests.Infrastructure.SQLServer;

internal static class InfrastructureTestData
{
    public static User User(Guid? id = null, string email = "user@example.com")
    {
        return new User
        {
            Id = id ?? Guid.NewGuid(),
            Email = email,
            PasswordHash = "password-hash",
            CreatedAtUtc = DateTime.UtcNow,
            RefreshTokens = []
        };
    }

    public static Deck Deck(
        long id,
        Guid userId,
        string name = "English",
        List<Note>? notes = null,
        int dailyNewNotesCount = 10,
        bool useOnlyLearnedNotes = true)
    {
        var deck = new Deck
        {
            Id = id,
            UserId = userId,
            Name = name,
            FrontLanguage = "English",
            BackLanguage = "Ukrainian",
            DeckDailyNewNotesCount = dailyNewNotesCount,
            UseOnlyLearnedNotes = useOnlyLearnedNotes,
            Notes = notes ?? []
        };

        foreach (var note in deck.Notes)
        {
            note.UserId = userId;
            note.DeckId = id;
        }

        return deck;
    }

    public static Note Note(
        long id,
        Guid userId,
        long deckId,
        string front = "dog",
        string back = "pes",
        bool isLearned = true,
        int learningPoints = 0,
        DateOnly? nextReviewDate = null)
    {
        return new Note
        {
            Id = id,
            UserId = userId,
            DeckId = deckId,
            Front = front,
            Back = back,
            IsLearned = isLearned,
            LearningPoints = learningPoints,
            NextReviewDate = nextReviewDate ?? DateOnly.MinValue
        };
    }
}
