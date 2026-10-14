using System;
using Application.Abstractions.Anki;

namespace Infrastructure.AnkiConnect
{
    internal class AnkiGetCardsId(AnkiConnectTransport transport) : IAnkiCardIds
    {
        public Task<long[]> GetCardsIdAsync(string deckName, CancellationToken token)
        {
            var result = transport.SendRequestAsync<long[]>("findCards", token, new { query = $"deck:{deckName.Trim().Replace(" ", "_")}" });
            return result;
        }
    }
}
