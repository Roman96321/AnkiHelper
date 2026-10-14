using System;

namespace Application.Abstractions.Anki
{
    public interface IAnkiCheckConnection
    {
        Task<bool> CheckConnectionAsync(CancellationToken token);
    }
}
