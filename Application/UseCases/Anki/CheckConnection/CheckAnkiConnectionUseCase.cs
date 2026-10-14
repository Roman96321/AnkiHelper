using Application.Abstractions.Anki;
using System;

namespace Application.UseCases.Anki.CheckConnection
{
    public class CheckAnkiConnectionUseCase(IAnkiCheckConnection ankiCheckConnection)
    {
        public async Task<bool> CheckConnectionAsync(CancellationToken token)
        {
            return await ankiCheckConnection.CheckConnectionAsync(token);
        }
    }
}
