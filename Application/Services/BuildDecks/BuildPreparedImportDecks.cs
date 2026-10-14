using Application.Abstractions.Anki;
using Application.UseCases.Decks.DeckImports.Models;
using Application.UseCases.Decks.DeckImports.PrepareConfiguration;
using System;

namespace Application.Services.BuildDecks
{
    public class BuildPreparedImportDecks(
        IAnkiNotesInfo getNotesInfo,
        IAnkiCardsInfo getCardsInfo,
        IAnkiCardIds getCardsId,
        IAnkiDeckNamesAndIds getDecksId) : IPreparedImportDecksBuilder
    {
        public async Task<List<PreparedImportDeck>> GetPreparedImportDecksAsync(List<string> deckNames, CancellationToken token)
        {
            if (deckNames == null || deckNames.Count == 0)
                throw new ArgumentException("At least one deck name is required.", nameof(deckNames));

            var preparedDecks = new List<PreparedImportDeck>();

            var deckIds = await getDecksId.GetDeckNamesAndIdsAsync(token);

            foreach (var deckName in deckNames)
            {
                token.ThrowIfCancellationRequested();

                var cardsId = await getCardsId.GetCardsIdAsync(deckName, token);
                var notes = await GetImportNotesAsync(cardsId, token);

                preparedDecks.Add(new PreparedImportDeck
                {
                    Id = deckIds[deckName],
                    Name = deckName,
                    Notes = notes
                });
            }

            return preparedDecks;
        }

        private List<PreparedImportNote> BuildImportNotes(List<AnkiNoteInfo> notes, List<AnkiCardInfo> cardsDto)
        {
            var result = new List<PreparedImportNote>();

            if (notes.Count == 0 || cardsDto.Count == 0)
                return result;

            var cardsDictionary = cardsDto
                .GroupBy(card => card.NoteId)
                .ToDictionary(group => group.Key, group => group.ToList());

            foreach (var note in notes)
            {
                var cards = cardsDictionary[note.NoteId];

                result.Add(new PreparedImportNote
                {
                    NoteId = note.NoteId,
                    Fields = note.Fields,
                    IsLearned = cards.All(x => x.Queue == 2)
                });
            }

            return result;
        }

        private async Task<List<PreparedImportNote>> GetImportNotesAsync(long[] cardsId, CancellationToken token)
        {
            var cards = await getCardsInfo.GetCardsInfoAsync(cardsId, token);
            var noteIds = cards.Select(card => card.NoteId).Distinct().ToArray();
            var notes = await getNotesInfo.GetNotesInfoAsync(noteIds, token);

            return BuildImportNotes(notes, cards);
        }
    }
}
