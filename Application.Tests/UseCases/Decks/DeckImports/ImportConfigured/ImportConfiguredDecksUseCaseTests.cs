using Application.Abstractions;
using Application.Services.ConfiguredDecks;
using Application.UseCases.Decks.DeckImports.ImportConfigured;
using Application.UseCases.Decks.DeckImports.Models;
using Domain.Deck;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using static Application.Tests.TestData.PreparedImportDeckFactory;

namespace Application.Tests.UseCases.Decks.DeckImports.ImportConfigured;

public class ImportConfiguredDecksUseCaseTests
{
    private readonly IMemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());
    private readonly IDeckRepository _repository = Substitute.For<IDeckRepository>();
    private readonly IConfiguredDeckImportBuilder _configuredDeckImportBuilder = Substitute.For<IConfiguredDeckImportBuilder>();

    [Fact]
    public async Task ImportAsync_WhenConfigurationIsValid_BuildsDecksAndSavesThem()
    {
        var userId = Guid.NewGuid();
        var preparedDecks = new List<PreparedImportDeck>
        {
            PreparedDeck(123, "English Words")
        };
        var builtDecks = new List<Deck>
        {
            new Deck { Id = 123, Name = "English Words" }
        };
        var configuredDecks = new List<ConfiguredDeckImport>
        {
            ConfiguredDeck(
                deckName: "English Words",
                noteTypes:
                [
                    new ConfiguredNoteImport { FrontKey = "Front", BackKey = "Back" },
                    new ConfiguredNoteImport { FrontKey = "Term", BackKey = "Translation" }
                ])
        };
        var expectedMappings = new (string FrontKey, string BackKey)[]
        {
            ("Front", "Back"),
            ("Term", "Translation")
        };

        CachePreparedDecks(userId, preparedDecks);

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

        await useCase.ImportAsync(configuredDecks, userId, CancellationToken.None);

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
            .ReplaceAsync(builtDecks, userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ImportAsync_WhenSomeDecksHaveNoNoteTypes_IgnoresEmptyDecks()
    {
        var userId = Guid.NewGuid();
        var preparedDecks = new List<PreparedImportDeck>
        {
            PreparedDeck(123, "English Words"),
            PreparedDeck(456, "Empty Deck")
        };
        var builtDecks = new List<Deck>
        {
            new Deck { Id = 123, Name = "English Words" }
        };
        var configuredDecks = new List<ConfiguredDeckImport>
        {
            ConfiguredDeck("English Words", [new ConfiguredNoteImport { FrontKey = "Front", BackKey = "Back" }]),
            ConfiguredDeck("Empty Deck", [])
        };

        CachePreparedDecks(userId, preparedDecks);

        _configuredDeckImportBuilder
            .Build(
                Arg.Is<List<DeckImportSelection>>(selections =>
                    selections.Count == 1 &&
                    selections.Single().DeckName == "English Words"),
                preparedDecks,
                Arg.Any<CancellationToken>())
            .Returns(builtDecks);

        var useCase = CreateUseCase();

        await useCase.ImportAsync(configuredDecks, userId, CancellationToken.None);

        await _repository.Received(1)
            .SaveDeckImportSettingsAsync(
                Arg.Is<List<DeckImportSelection>>(selections =>
                    selections.Count == 1 &&
                    selections.Single().DeckName == "English Words"),
                userId,
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ImportAsync_WhenAllDecksHaveNoNoteTypes_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var configuredDecks = new List<ConfiguredDeckImport>
        {
            ConfiguredDeck("English Words", [])
        };
        var useCase = CreateUseCase();

        var act = () => useCase.ImportAsync(configuredDecks, userId, CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("At least one deck import configuration must contain note types.");

        await _configuredDeckImportBuilder.DidNotReceiveWithAnyArgs()
            .Build(default!, default!, default);

        await _repository.DidNotReceiveWithAnyArgs()
            .SaveDeckImportSettingsAsync(default!, default, default);

        await _repository.DidNotReceiveWithAnyArgs()
            .ReplaceAsync(default!, default, default);
    }

    [Fact]
    public async Task ImportAsync_WhenPreparedDecksCacheIsEmpty_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid();
        var configuredDecks = new List<ConfiguredDeckImport>
        {
            ConfiguredDeck("English Words", [new ConfiguredNoteImport { FrontKey = "Front", BackKey = "Back" }])
        };
        var useCase = CreateUseCase();

        var act = () => useCase.ImportAsync(configuredDecks, userId, CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("No decks to import. Please prepare the decks before importing.");

        await _configuredDeckImportBuilder.DidNotReceiveWithAnyArgs()
            .Build(default!, default!, default);

        await _repository.DidNotReceiveWithAnyArgs()
            .SaveDeckImportSettingsAsync(default!, default, default);

        await _repository.DidNotReceiveWithAnyArgs()
            .ReplaceAsync(default!, default, default);
    }

    [Fact]
    public async Task ImportAsync_UsesUserSpecificPreparedDecksCache()
    {
        var targetUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var targetPreparedDecks = new List<PreparedImportDeck>
        {
            PreparedDeck(123, "English Words")
        };
        var otherPreparedDecks = new List<PreparedImportDeck>
        {
            PreparedDeck(456, "English Words")
        };
        var builtDecks = new List<Deck>
        {
            new Deck { Id = 123, Name = "English Words" }
        };
        var configuredDecks = new List<ConfiguredDeckImport>
        {
            ConfiguredDeck("English Words", [new ConfiguredNoteImport { FrontKey = "Front", BackKey = "Back" }])
        };

        CachePreparedDecks(otherUserId, otherPreparedDecks);
        CachePreparedDecks(targetUserId, targetPreparedDecks);

        _configuredDeckImportBuilder
            .Build(
                Arg.Any<List<DeckImportSelection>>(),
                targetPreparedDecks,
                Arg.Any<CancellationToken>())
            .Returns(builtDecks);

        var useCase = CreateUseCase();

        await useCase.ImportAsync(configuredDecks, targetUserId, CancellationToken.None);

        await _configuredDeckImportBuilder.Received(1)
            .Build(
                Arg.Is<List<DeckImportSelection>>(selections => selections.Single().DeckId == 123),
                targetPreparedDecks,
                Arg.Any<CancellationToken>());
    }

    private ImportConfiguredDecksUseCase CreateUseCase()
    {
        return new ImportConfiguredDecksUseCase(
            _memoryCache,
            _repository,
            _configuredDeckImportBuilder);
    }

    private void CachePreparedDecks(Guid userId, List<PreparedImportDeck> preparedDecks)
    {
        _memoryCache.Set($"deck-import:{userId}", preparedDecks);
    }

    private static ConfiguredDeckImport ConfiguredDeck(string deckName, List<ConfiguredNoteImport> noteTypes)
    {
        return new ConfiguredDeckImport
        {
            DeckName = deckName,
            NoteTypes = noteTypes
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
