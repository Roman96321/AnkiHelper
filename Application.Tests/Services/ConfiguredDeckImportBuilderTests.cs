using Application.Abstractions.OpenAI;
using Application.Services.ConfiguredDecks;
using Application.UseCases.Decks.DeckImports.Models;
using FluentAssertions;
using NSubstitute;
using static Application.Tests.TestData.PreparedImportDeckFactory;

namespace Application.Tests.Services;

public class ConfiguredDeckImportBuilderTests
{
    private readonly IDetermineLanguage _determineLanguage = Substitute.For<IDetermineLanguage>();

    [Fact]
    public async Task Build_WhenDeckHasMatchingFieldMappings_ReturnsConfiguredDeck()
    {
        var deckImportSelection = new DeckImportSelection
        {
            DeckName = "English Words",
            DeckId = 123,
            FieldMappings =
            [
                new DeckImportFieldSelection { FrontKey = "FrontField", BackKey = "BackField" },
                new DeckImportFieldSelection { FrontKey = "Front", BackKey = "Back" }
            ]
        };

        var preparedDecks = new List<PreparedImportDeck>
        {
            PreparedDeck(
                id: 123,
                name: "English Words",
                notes:
                [
                    PreparedNote(
                        noteId: 1,
                        isLearned: false,
                        fields: new Dictionary<string, AnkiField>
                        {
                            ["FrontField"] = Field("Hello", 0),
                            ["BackField"] = Field("Hola", 1)
                        }),
                    PreparedNote(
                        noteId: 2,
                        isLearned: true,
                        fields: new Dictionary<string, AnkiField>
                        {
                            ["Front"] = Field("Goodbye", 0),
                            ["Back"] = Field("Adios", 1)
                        })
                ])
        };

        ReturnsLanguage(["Hello", "Goodbye"], "English");
        ReturnsLanguage(["Hola", "Adios"], "Spanish");

        var service = CreateService();

        var result = await service.Build([deckImportSelection], preparedDecks, CancellationToken.None);

        result.Should().ContainSingle();

        var deck = result.Single();
        deck.Id.Should().Be(123);
        deck.Name.Should().Be("English Words");
        deck.Notes.Should().HaveCount(2);
        deck.BackLanguage.Should().Be("Spanish");
        deck.FrontLanguage.Should().Be("English");
        deck.DeckDailyNewNotesCount.Should().Be(10);
        deck.UseOnlyLearnedNotes.Should().BeTrue();

        var note1 = deck.Notes.Single(n => n.Id == 1);
        note1.IsLearned.Should().BeFalse();
        note1.Id.Should().Be(1);
        note1.DeckId.Should().Be(123);
        note1.Front.Should().Be("Hello");
        note1.Back.Should().Be("Hola");
        note1.LearningPoints.Should().Be(0);
        note1.NextReviewDate.Should().Be(DateOnly.MinValue);

        var note2 = deck.Notes.Single(n => n.Id == 2);
        note2.IsLearned.Should().BeTrue();
        note2.Id.Should().Be(2);
        note2.DeckId.Should().Be(123);
        note2.Front.Should().Be("Goodbye");
        note2.Back.Should().Be("Adios");
        note2.LearningPoints.Should().Be(0);
        note2.NextReviewDate.Should().Be(DateOnly.MinValue);
    }

    [Fact]
    public async Task Build_WhenDeckIdIsZero_FindsPreparedDeckByName()
    {
        var deckImportSelection = new DeckImportSelection
        {
            DeckName = "English Words",
            DeckId = 0,
            FieldMappings =
            [
                new DeckImportFieldSelection { FrontKey = "Front", BackKey = "Back" }
            ]
        };
        var preparedDecks = new List<PreparedImportDeck>
        {
            PreparedDeck(123, "English Words", [PreparedNote(1, true)])
        };

        ReturnsLanguage(["dog"], "English");
        ReturnsLanguage(["pes"], "Polish");

        var service = CreateService();

        var result = await service.Build([deckImportSelection], preparedDecks, CancellationToken.None);

        result.Single().Id.Should().Be(123);
        result.Single().Name.Should().Be("English Words");
    }

    [Fact]
    public async Task Build_WhenFieldMappingIsDuplicated_DoesNotDuplicateNotes()
    {
        var deckImportSelection = new DeckImportSelection
        {
            DeckName = "English Words",
            DeckId = 123,
            FieldMappings =
            [
                new DeckImportFieldSelection { FrontKey = "Front", BackKey = "Back" },
                new DeckImportFieldSelection { FrontKey = "Front", BackKey = "Back" }
            ]
        };
        var preparedDecks = new List<PreparedImportDeck>
        {
            PreparedDeck(123, "English Words", [PreparedNote(1, true)])
        };

        ReturnsLanguage(["dog"], "English");
        ReturnsLanguage(["pes"], "Polish");

        var service = CreateService();

        var result = await service.Build([deckImportSelection], preparedDecks, CancellationToken.None);

        result.Single().Notes.Should().ContainSingle();
    }

    [Fact]
    public async Task Build_WhenNoteDoesNotContainBothMappedFields_SkipsNote()
    {
        var deckImportSelection = new DeckImportSelection
        {
            DeckName = "English Words",
            DeckId = 123,
            FieldMappings =
            [
                new DeckImportFieldSelection { FrontKey = "Front", BackKey = "Back" }
            ]
        };
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
                            ["Front"] = Field("dog", 0)
                        }),
                    PreparedNote(
                        noteId: 2,
                        isLearned: true,
                        fields: new Dictionary<string, AnkiField>
                        {
                            ["Front"] = Field("cat", 0),
                            ["Back"] = Field("kit", 1)
                        })
                ])
        };

        ReturnsLanguage(["cat"], "English");
        ReturnsLanguage(["kit"], "Polish");

        var service = CreateService();

        var result = await service.Build([deckImportSelection], preparedDecks, CancellationToken.None);

        result.Single().Notes.Should().ContainSingle();
        result.Single().Notes.Single().Id.Should().Be(2);
    }

    [Fact]
    public async Task Build_WhenFieldsContainHtmlAndSoundTags_CleansNoteTextBeforeDetectingLanguages()
    {
        var deckImportSelection = new DeckImportSelection
        {
            DeckName = "English Words",
            DeckId = 123,
            FieldMappings =
            [
                new DeckImportFieldSelection { FrontKey = "Front", BackKey = "Back" }
            ]
        };
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
                            ["Front"] = Field("<div>dog&nbsp;</div>[sound:dog.mp3]", 0),
                            ["Back"] = Field("<img src=\"x.jpg\" /><div>pes</div>", 1)
                        })
                ])
        };

        ReturnsLanguage(["dog"], "English");
        ReturnsLanguage(["pes"], "Polish");

        var service = CreateService();

        var result = await service.Build([deckImportSelection], preparedDecks, CancellationToken.None);

        var note = result.Single().Notes.Single();
        note.Front.Should().Be("dog");
        note.Back.Should().Be("pes");
        result.Single().FrontLanguage.Should().Be("English");
        result.Single().BackLanguage.Should().Be("Polish");
    }

    [Fact]
    public async Task Build_WhenNoNotesAreImported_UsesUndLanguageAndDoesNotCallLanguageDetector()
    {
        var deckImportSelection = new DeckImportSelection
        {
            DeckName = "English Words",
            DeckId = 123,
            FieldMappings =
            [
                new DeckImportFieldSelection { FrontKey = "MissingFront", BackKey = "MissingBack" }
            ]
        };
        var preparedDecks = new List<PreparedImportDeck>
        {
            PreparedDeck(123, "English Words", [PreparedNote(1, true)])
        };

        var service = CreateService();

        var result = await service.Build([deckImportSelection], preparedDecks, CancellationToken.None);

        var deck = result.Single();
        deck.Notes.Should().BeEmpty();
        deck.FrontLanguage.Should().Be("und");
        deck.BackLanguage.Should().Be("und");

        await _determineLanguage.DidNotReceiveWithAnyArgs()
            .Determine(default!, default);
    }

    private ConfiguredDeckImportBuilder CreateService()
    {
        return new ConfiguredDeckImportBuilder(_determineLanguage);
    }

    private void ReturnsLanguage(string[] words, string language)
    {
        _determineLanguage.Determine(
                Arg.Is<string[]>(actualWords => actualWords.SequenceEqual(words)),
                Arg.Any<CancellationToken>())
            .Returns(language);
    }
}
