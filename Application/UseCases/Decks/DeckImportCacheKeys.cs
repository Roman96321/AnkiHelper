using System;

namespace Application.UseCases.Decks
{
    internal static class DeckImportCacheKeys
    {
        public static string ImportDecks(Guid userId)
        {
            return $"deck-import:{userId}";
        }
    }
}
