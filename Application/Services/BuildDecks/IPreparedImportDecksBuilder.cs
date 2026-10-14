using Application.UseCases.Decks.DeckImports.Models;
using Application.UseCases.Decks.DeckImports.PrepareConfiguration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Services.BuildDecks
{
    public interface IPreparedImportDecksBuilder
    {
        Task<List<PreparedImportDeck>> GetPreparedImportDecksAsync(List<string> deckNames, CancellationToken token);
    }
}
