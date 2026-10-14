using Application.Abstractions;
using Application.Abstractions.Anki;
using Application.Services.BuildDecks;
using Application.Services.ConfiguredDecks;
using Application.UseCases.Decks.DeckImports.Models;
using Application.UseCases.Decks.Synchronization;
using Domain.Deck;
using FluentAssertions;
using NSubstitute;
using static Application.Tests.TestData.PreparedImportDeckFactory;

namespace Application.Tests.UseCases.Decks.Synchronization;

public class DeckSynchronizationUseCaseTests
{
    private readonly IDeckRepository _repository = Substitute.For<IDeckRepository>();
    private readonly IPreparedImportDecksBuilder _importDecksBuilder = Substitute.For<IPreparedImportDecksBuilder>();
    private readonly IConfiguredDeckImportBuilder _configuredDeckImportBuilder = Substitute.For<IConfiguredDeckImportBuilder>();
    private readonly IAnkiDeckNamesAndIds _getDecksId = Substitute.For<IAnkiDeckNamesAndIds>();

    [Fact]
    public async Task SynchronizeDecksAsync_WhenUserHasNoDecks_DoesNothing()
    {
        var userId = Guid.NewGuid();

        _repository.GetAllDeckNamesAndIdsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, long>());

        var useCase = CreateUseCase();

        await useCase.SynchronizeDecksAsync(userId, CancellationToken.None);

        await _getDecksId.DidNotReceiveWithAnyArgs()
            .GetDeckNamesAndIdsAsync(default);

        await _importDecksBuilder.DidNotReceiveWithAnyArgs()
            .GetPreparedImportDecksAsync(default!, default);

        await _configuredDeckImportBuilder.DidNotReceiveWithAnyArgs()
            .Build(default!, default!, default);

        await _repository.DidNotReceiveWithAnyArgs()
            .ReplaceAllAsync(default!, default, default);
    }

    [Fact]
    public async Task SynchronizeDecksAsync_WhenDeckWasRenamed_UsesCurrentDeckNameFromAnki()
    {
        var userId = Guid.NewGuid();
        var preparedDecks = new List<PreparedImportDeck>
        {
            PreparedDeck(123, "New name", [PreparedNote(1, true)])
        };
        var deckSettings = new List<DeckImportSettings>
        {
            Settings(123, "Old name", [Mapping("Front", "Back")])
        };
        var builtDecks = new List<Deck>
        {
            new Deck { Id = 123, Name = "New name" }
        };

        _repository.GetAllDeckNamesAndIdsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, long>
            {
                ["Old name"] = 123
            });

        _getDecksId.GetDeckNamesAndIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, long>
            {
                ["New name"] = 123
            });

        _importDecksBuilder
            .GetPreparedImportDecksAsync(
                Arg.Is<List<string>>(deckNames => deckNames.SequenceEqual(new[] { "New name" })),
                Arg.Any<CancellationToken>())
            .Returns(preparedDecks);

        _repository.GetDeckImportSettingsAsync(
                Arg.Is<List<long>>(deckIds => deckIds.SequenceEqual(new long[] { 123 })),
                userId,
                Arg.Any<CancellationToken>())
            .Returns(deckSettings);

        _configuredDeckImportBuilder
            .Build(Arg.Any<List<DeckImportSelection>>(), preparedDecks, Arg.Any<CancellationToken>())
            .Returns(builtDecks);

        var useCase = CreateUseCase();

        await useCase.SynchronizeDecksAsync(userId, CancellationToken.None);

        await _importDecksBuilder.Received(1)
            .GetPreparedImportDecksAsync(
                Arg.Is<List<string>>(deckNames => deckNames.SequenceEqual(new[] { "New name" })),
                Arg.Any<CancellationToken>());

        await _repository.Received(1)
            .SaveDeckImportSettingsAsync(
                Arg.Is<List<DeckImportSelection>>(selections =>
                    selections.Count == 1 &&
                    selections.Single().DeckId == 123 &&
                    selections.Single().DeckName == "New name"),
                userId,
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SynchronizeDecksAsync_WhenSettingsExist_RebuildsSelectionsAndReplacesDecks()
    {
        var userId = Guid.NewGuid();
        var preparedDecks = new List<PreparedImportDeck>
        {
            PreparedDeck(123, "English Words", [PreparedNote(1, true)])
        };
        var deckSettings = new List<DeckImportSettings>
        {
            Settings(
                deckId: 123,
                deckName: "English Words",
                fieldMappings:
                [
                    Mapping("Front", "Back"),
                    Mapping("Term", "Translation")
                ])
        };
        var builtDecks = new List<Deck>
        {
            new Deck { Id = 123, Name = "English Words" }
        };
        var expectedMappings = new (string FrontKey, string BackKey)[]
        {
            ("Front", "Back"),
            ("Term", "Translation")
        };

        _repository.GetAllDeckNamesAndIdsAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, long>
            {
                ["English Words"] = 123
            });

        _getDecksId.GetDeckNamesAndIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, long>
            {
                ["English Words"] = 123
            });

        _importDecksBuilder.GetPreparedImportDecksAsync(
                Arg.Is<List<string>>(deckNames => deckNames.SequenceEqual(new[] { "English Words" })),
                Arg.Any<CancellationToken>())
            .Returns(preparedDecks);

        _repository.GetDeckImportSettingsAsync(
                Arg.Is<List<long>>(deckIds => deckIds.SequenceEqual(new long[] { 123 })),
                userId,
                Arg.Any<CancellationToken>())
            .Returns(deckSettings);

        _configuredDeckImportBuilder
            .Build(
                Arg.Is<List<DeckImportSelection>>(selections => MatchesSelection(
                    selections,
                    deckId: 123,
                    deckName: "English Words",
                    expectedMappings)),
                preparedDecks,
                Arg.Any<CancellationToken>())
            .Returns(builtDecks);

        var useCase = CreateUseCase();

        await useCase.SynchronizeDecksAsync(userId, CancellationToken.None);

        await _configuredDeckImportBuilder.Received(1)
            .Build(
                Arg.Is<List<DeckImportSelection>>(selections => MatchesSelection(
                    selections,
                    deckId: 123,
                    deckName: "English Words",
                    expectedMappings)),
                preparedDecks,
                Arg.Any<CancellationToken>());

        await _repository.Received(1)
            .SaveDeckImportSettingsAsync(
                Arg.Is<List<DeckImportSelection>>(selections => MatchesSelection(
                    selections,
                    deckId: 123,
                    deckName: "English Words",
                    expectedMappings)),
                userId,
                Arg.Any<CancellationToken>());

        await _repository.Received(1)
            .ReplaceAllAsync(builtDecks, userId, Arg.Any<CancellationToken>());
    }

    private DeckSynchronizationUseCase CreateUseCase()
    {
        return new DeckSynchronizationUseCase(
            _repository,
            _importDecksBuilder,
            _configuredDeckImportBuilder,
            _getDecksId);
    }

    private static DeckImportSettings Settings(long deckId, string deckName, List<DeckImportFieldMapping> fieldMappings)
    {
        return new DeckImportSettings
        {
            DeckId = deckId,
            DeckName = deckName,
            FieldMappings = fieldMappings
        };
    }

    private static DeckImportFieldMapping Mapping(string frontKey, string backKey)
    {
        return new DeckImportFieldMapping
        {
            FrontKey = frontKey,
            BackKey = backKey
        };
    }

    private static bool MatchesSelection(
        List<DeckImportSelection> selections,
        long deckId,
        string deckName,
        (string FrontKey, string BackKey)[] expectedMappings)
    {
        if (selections.Count != 1)
            return false;

        var selection = selections.Single();

        return selection.DeckId == deckId &&
               selection.DeckName == deckName &&
               selection.FieldMappings
                   .Select(mapping => (mapping.FrontKey, mapping.BackKey))
                   .SequenceEqual(expectedMappings);
    }
}
