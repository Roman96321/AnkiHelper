using Application.Services.BuildDecks;
using Application.UseCases.Decks.DeckImports.Models;
using Application.UseCases.Decks.DeckImports.PrepareConfiguration;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using static Application.Tests.TestData.PreparedImportDeckFactory;

namespace Application.Tests.UseCases.Decks.DeckImports.PrepareConfiguration;

public class PrepareDeckImportConfigurationUseCaseTests
{
    private readonly IPreparedImportDecksBuilder _importDecksBuilder = Substitute.For<IPreparedImportDecksBuilder>();
    private readonly IMemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());

    [Fact]
    public async Task PrepareConfigurationAsync_WhenDeckNamesAreValid_ReturnsConfigurationAndCachesPreparedDecks()
    {
        var userId = Guid.NewGuid();
        var deckNames = new List<string> { "English Words" };
        var preparedDecks = new List<PreparedImportDeck>
        {
            PreparedDeck(
                id: 123,
                name: "English Words",
                notes:
                [
                    PreparedNote(
                        noteId: 1,
                        isLearned: true,
                        fields: new Dictionary<string, AnkiField>
                        {
                            ["Front"] = Field("dog", 0),
                            ["Back"] = Field("pes", 1)
                        })
                ])
        };

        _importDecksBuilder
            .GetPreparedImportDecksAsync(deckNames, Arg.Any<CancellationToken>())
            .Returns(preparedDecks);

        var useCase = CreateUseCase();

        var result = await useCase.PrepareConfigurationAsync(deckNames, userId, CancellationToken.None);

        result.Should().ContainSingle();
        result.Single().Name.Should().Be("English Words");
        result.Single().Notes.Should().ContainSingle();
        result.Single().Notes.Single().Fields.Should().Equal(new Dictionary<string, string>
        {
            ["Front"] = "dog",
            ["Back"] = "pes"
        });

        _memoryCache.TryGetValue($"deck-import:{userId}", out List<PreparedImportDeck>? cachedDecks)
            .Should()
            .BeTrue();

        cachedDecks.Should().BeSameAs(preparedDecks);
    }

    [Fact]
    public async Task PrepareConfigurationAsync_UsesUserSpecificCacheKey()
    {
        var targetUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var targetPreparedDecks = new List<PreparedImportDeck>
        {
            PreparedDeck(123, "English Words", [PreparedNote(1, true)])
        };

        _memoryCache.Set($"deck-import:{otherUserId}", new List<PreparedImportDeck>
        {
            PreparedDeck(456, "Other Words", [PreparedNote(2, true)])
        });

        _importDecksBuilder
            .GetPreparedImportDecksAsync(Arg.Any<List<string>>(), Arg.Any<CancellationToken>())
            .Returns(targetPreparedDecks);

        var useCase = CreateUseCase();

        await useCase.PrepareConfigurationAsync(["English Words"], targetUserId, CancellationToken.None);

        _memoryCache.TryGetValue($"deck-import:{targetUserId}", out List<PreparedImportDeck>? targetCachedDecks)
            .Should()
            .BeTrue();

        _memoryCache.TryGetValue($"deck-import:{otherUserId}", out List<PreparedImportDeck>? otherCachedDecks)
            .Should()
            .BeTrue();

        targetCachedDecks.Should().BeSameAs(targetPreparedDecks);
        otherCachedDecks.Should().NotBeSameAs(targetPreparedDecks);
    }

    [Fact]
    public async Task PrepareConfigurationAsync_WhenPreparedDeckBuilderThrows_DoesNotCachePreparedDecks()
    {
        var userId = Guid.NewGuid();
        var expectedException = new ArgumentException("At least one deck name is required.", "deckNames");

        _importDecksBuilder
            .GetPreparedImportDecksAsync(Arg.Any<List<string>>(), Arg.Any<CancellationToken>())
            .Returns<Task<List<PreparedImportDeck>>>(_ => throw expectedException);

        var useCase = CreateUseCase();

        var act = () => useCase.PrepareConfigurationAsync([], userId, CancellationToken.None);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithParameterName("deckNames");

        _memoryCache.TryGetValue($"deck-import:{userId}", out _)
            .Should()
            .BeFalse();
    }

    private PrepareDeckImportConfigurationUseCase CreateUseCase()
    {
        return new PrepareDeckImportConfigurationUseCase(
            _importDecksBuilder,
            _memoryCache);
    }
}
