using System;

namespace Application.Abstractions.Anki
{
    public interface IAnkiDeckNamesAndIds
    {
        Task<Dictionary<string, long>> GetDeckNamesAndIdsAsync(CancellationToken token);
    }
}