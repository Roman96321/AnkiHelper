using Application.Abstractions.Anki;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.AnkiConnect
{
    internal class AnkiGetDeckNamesAndIds(AnkiConnectTransport transport) : IAnkiDeckNamesAndIds
    {
        
        public async Task<Dictionary<string, long>> GetDeckNamesAndIdsAsync(CancellationToken token)
        {
            var result = await transport.SendRequestAsync<Dictionary<string, long>>("deckNamesAndIds", token);
            return result;
        }
    }
}
