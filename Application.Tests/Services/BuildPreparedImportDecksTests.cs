using Application.Abstractions.Anki;
using Application.Services.BuildDecks;
using Application.UseCases.Decks.DeckImports.Models;
using FluentAssertions;
using NSubstitute;

namespace Application.Tests.Services;

public class BuildPreparedImportDecksTests
{
    private readonly IAnkiNotesInfo _notesInfo = Substitute.For<IAnkiNotesInfo>();
    private readonly IAnkiCardsInfo _cardsInfo = Substitute.For<IAnkiCardsInfo>();
    private readonly IAnkiCardIds _cardIds = Substitute.For<IAnkiCardIds>();
    private readonly IAnkiDeckNamesAndIds _deckNamesAndIds = Substitute.For<IAnkiDeckNamesAndIds>();

    [Fact]
    public async Task GetPreparedImportDecksAsync_WhenDeckHasCardsAndNotes_ReturnsPreparedDeck()
    {
        _deckNamesAndIds.GetDeckNamesAndIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, long>
            {
                ["English"] = 123
            });

        _cardIds.GetCardsIdAsync("English", Arg.Any<CancellationToken>())
            .Returns([10, 11]);

        _cardsInfo.GetCardsInfoAsync(
                Arg.Is<long[]>(ids => ids.SequenceEqual(new long[] { 10, 11 })),
                Arg.Any<CancellationToken>())
            .Returns([
                Card(noteId: 100, queue: 2),
                Card(noteId: 100, queue: 2)
            ]);

        _notesInfo.GetNotesInfoAsync(
                Arg.Is<long[]>(ids => ids.SequenceEqual(new long[] { 100 })),
                Arg.Any<CancellationToken>())
            .Returns([
                Note(100, "dog", "пес")
            ]);

        var service = CreateService();

        var result = await service.GetPreparedImportDecksAsync(["English"], CancellationToken.None);

        result.Should().ContainSingle();

        var deck = result.Single();
        deck.Id.Should().Be(123);
        deck.Name.Should().Be("English");
        deck.Notes.Should().ContainSingle();

        var note = deck.Notes.Single();
        note.NoteId.Should().Be(100);
        note.IsLearned.Should().BeTrue();
        note.Fields["Front"].Value.Should().Be("dog");
        note.Fields["Back"].Value.Should().Be("пес");
    }

    [Fact]
    public async Task GetPreparedImportDecksAsync_WhenAnyCardForNoteIsNotLearned_ReturnsNoteAsNotLearned()
    {
        _deckNamesAndIds.GetDeckNamesAndIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, long>
            {
                ["English"] = 123
            });

        _cardIds.GetCardsIdAsync("English", Arg.Any<CancellationToken>())
            .Returns([10, 11]);

        _cardsInfo.GetCardsInfoAsync(Arg.Any<long[]>(), Arg.Any<CancellationToken>())
            .Returns([
                Card(noteId: 100, queue: 2),
                Card(noteId: 100, queue: 0)
            ]);

        _notesInfo.GetNotesInfoAsync(Arg.Any<long[]>(), Arg.Any<CancellationToken>())
            .Returns([
                Note(100, "dog", "пес")
            ]);

        var service = CreateService();

        var result = await service.GetPreparedImportDecksAsync(["English"], CancellationToken.None);

        result.Single().Notes.Single().IsLearned.Should().BeFalse();
    }

    [Fact]
    public async Task GetPreparedImportDecksAsync_WhenCardsContainDuplicateNoteIds_LoadsNoteInfoOnce()
    {
        _deckNamesAndIds.GetDeckNamesAndIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, long>
            {
                ["English"] = 123
            });

        _cardIds.GetCardsIdAsync("English", Arg.Any<CancellationToken>())
            .Returns([10, 11, 12]);

        _cardsInfo.GetCardsInfoAsync(Arg.Any<long[]>(), Arg.Any<CancellationToken>())
            .Returns([
                Card(noteId: 100, queue: 2),
                Card(noteId: 100, queue: 2),
                Card(noteId: 200, queue: 2)
            ]);

        _notesInfo.GetNotesInfoAsync(
                Arg.Is<long[]>(ids => ids.SequenceEqual(new long[] { 100, 200 })),
                Arg.Any<CancellationToken>())
            .Returns([
                Note(100, "dog", "пес"),
                Note(200, "cat", "кіт")
            ]);

        var service = CreateService();

        var result = await service.GetPreparedImportDecksAsync(["English"], CancellationToken.None);

        result.Single().Notes.Select(note => note.NoteId)
            .Should()
            .Equal(100, 200);

        await _notesInfo.Received(1).GetNotesInfoAsync(
            Arg.Is<long[]>(ids => ids.SequenceEqual(new long[] { 100, 200 })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPreparedImportDecksAsync_WhenMultipleDecksRequested_ReturnsPreparedDecksInInputOrder()
    {
        _deckNamesAndIds.GetDeckNamesAndIdsAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, long>
            {
                ["English"] = 123,
                ["Spanish"] = 456
            });

        _cardIds.GetCardsIdAsync("English", Arg.Any<CancellationToken>())
            .Returns([10]);

        _cardIds.GetCardsIdAsync("Spanish", Arg.Any<CancellationToken>())
            .Returns([20]);

        _cardsInfo.GetCardsInfoAsync(
                Arg.Is<long[]>(ids => ids.SequenceEqual(new long[] { 10 })),
                Arg.Any<CancellationToken>())
            .Returns([Card(noteId: 100, queue: 2)]);

        _cardsInfo.GetCardsInfoAsync(
                Arg.Is<long[]>(ids => ids.SequenceEqual(new long[] { 20 })),
                Arg.Any<CancellationToken>())
            .Returns([Card(noteId: 200, queue: 2)]);

        _notesInfo.GetNotesInfoAsync(
                Arg.Is<long[]>(ids => ids.SequenceEqual(new long[] { 100 })),
                Arg.Any<CancellationToken>())
            .Returns([Note(100, "dog", "пес")]);

        _notesInfo.GetNotesInfoAsync(
                Arg.Is<long[]>(ids => ids.SequenceEqual(new long[] { 200 })),
                Arg.Any<CancellationToken>())
            .Returns([Note(200, "hola", "привіт")]);

        var service = CreateService();

        var result = await service.GetPreparedImportDecksAsync(["English", "Spanish"], CancellationToken.None);

        result.Select(deck => deck.Id).Should().Equal(123, 456);
        result.Select(deck => deck.Name).Should().Equal("English", "Spanish");
    }

    [Fact]
    public async Task GetPreparedImportDecksAsync_WhenDeckNamesAreEmpty_ThrowsArgumentException()
    {
        var service = CreateService();

        var act = () => service.GetPreparedImportDecksAsync([], CancellationToken.None);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithParameterName("deckNames");
    }

    [Fact]
    public async Task GetPreparedImportDecksAsync_WhenDeckNamesAreNull_ThrowsArgumentException()
    {
        var service = CreateService();

        var act = () => service.GetPreparedImportDecksAsync(null!, CancellationToken.None);

        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithParameterName("deckNames");
    }

    private BuildPreparedImportDecks CreateService()
    {
        return new BuildPreparedImportDecks(
            _notesInfo,
            _cardsInfo,
            _cardIds,
            _deckNamesAndIds);
    }

    private static AnkiCardInfo Card(long noteId, int queue)
    {
        return new AnkiCardInfo
        {
            NoteId = noteId,
            Queue = queue
        };
    }

    private static AnkiNoteInfo Note(long noteId, string front, string back)
    {
        return new AnkiNoteInfo
        {
            NoteId = noteId,
            Fields = new Dictionary<string, AnkiField>
            {
                ["Front"] = new AnkiField { Value = front, Order = 0 },
                ["Back"] = new AnkiField { Value = back, Order = 1 }
            }
        };
    }
}
