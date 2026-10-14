using System;
using Application.Abstractions.Anki;

namespace Infrastructure.AnkiConnect
{
    internal class AnkiCheckConnection(AnkiConnectTransport transport) : IAnkiCheckConnection
    {
        public async Task<bool> CheckConnectionAsync(CancellationToken token)
        {
            try
            {
                await transport.SendRequestAsync<int>("version", token);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            return true;
        }
    }
}
