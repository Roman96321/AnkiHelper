namespace Application.UseCases.Decks.Synchronization;

public class DeckNamesVerification
{
    public List<string> Verify(Dictionary<string, long> oldDeckNames, Dictionary<string, long> newDeckNames)
    {
        var currentDeckNamesById = newDeckNames
            .ToDictionary(deck => deck.Value, deck => deck.Key);

        var result = new List<string>();

        foreach (var oldDeck in oldDeckNames)
        {
            if (currentDeckNamesById.TryGetValue(oldDeck.Value, out var currentDeckName))
                result.Add(currentDeckName);
        }

        return result;
    }
}