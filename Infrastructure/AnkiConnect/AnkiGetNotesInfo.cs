using Application.Abstractions.Anki;
using Application.UseCases.Decks.DeckImports.Models;
using Infrastructure.AnkiConnect.DTOs;

namespace Infrastructure.AnkiConnect
{
    internal class AnkiGetNotesInfo(AnkiConnectTransport transport) : IAnkiNotesInfo
    {
        public async Task<List<AnkiNoteInfo>> GetNotesInfoAsync(long[] noteIds, CancellationToken token)
        {
            var result = new List<AnkiNoteInfo>();

            foreach (var note in noteIds.Chunk(300))
            {
                token.ThrowIfCancellationRequested();

                var noteIdsChunk = note.ToArray();
                var resultJson = await transport.SendRequestAsync<List<NoteInfoDto>>("notesInfo", token, new { notes = noteIdsChunk });

                foreach (var noteInfo in resultJson)
                {
                    result.Add(new AnkiNoteInfo
                    {
                        NoteId = noteInfo.NoteId,
                        Fields = noteInfo.Fields.ToDictionary(x => x.Key, x => new AnkiField
                        {
                            Value = x.Value.Value,
                            Order = x.Value.Order
                        })
                    });
                }
            }

            return result;
        }
    }
}
