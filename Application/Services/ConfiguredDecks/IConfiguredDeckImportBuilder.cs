using Application.UseCases.Decks.DeckImports.Models;
using Domain.Deck;
using System.Text;

namespace Application.Services.ConfiguredDecks
{
    public interface IConfiguredDeckImportBuilder
    {
        Task<List<Deck>> Build(List<DeckImportSelection> deckImportSelections, List<PreparedImportDeck> preparedDecks, CancellationToken token);
    }
}
