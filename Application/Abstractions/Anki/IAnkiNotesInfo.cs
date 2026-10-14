using Application.UseCases.Decks.DeckImports.Models;

namespace Application.Abstractions.Anki
{
    public interface IAnkiNotesInfo
    {
        Task<List<AnkiNoteInfo>> GetNotesInfoAsync(long[] noteIds, CancellationToken token);
    }
}
