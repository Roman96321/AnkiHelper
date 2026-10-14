using Application.UseCases.Decks.DeckImports.Models;

namespace Application.Tests.TestData;

internal static class PreparedImportDeckFactory
{
    public static PreparedImportDeck PreparedDeck(long id, string name, List<PreparedImportNote>? notes = null)
    {
        return new PreparedImportDeck
        {
            Id = id,
            Name = name,
            Notes = notes ?? []
        };
    }

    public static PreparedImportNote PreparedNote(long noteId, bool isLearned)
    {
        return PreparedNote(
            noteId,
            isLearned,
            new Dictionary<string, AnkiField>
            {
                ["Front"] = Field("dog", 0),
                ["Back"] = Field("pes", 1)
            });
    }

    public static PreparedImportNote PreparedNote(
        long noteId,
        bool isLearned,
        Dictionary<string, AnkiField> fields)
    {
        return new PreparedImportNote
        {
            NoteId = noteId,
            IsLearned = isLearned,
            Fields = fields
        };
    }

    public static AnkiField Field(string value, int order)
    {
        return new AnkiField
        {
            Value = value,
            Order = order
        };
    }
}
