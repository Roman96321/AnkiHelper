using Application.UseCases.Decks.DeckImports.Models;
using Application.UseCases.Decks.DeckImports.PrepareConfiguration;
using FluentAssertions;
using static Application.Tests.TestData.PreparedImportDeckFactory;

namespace Application.Tests.UseCases.Decks.DeckImports.PrepareConfiguration;

public class DeckImportConfigurationBuilderTests
{
    [Fact]
    public void Build_WhenDeckHasDifferentNoteFieldSets_ReturnsOneConfigurationPerFieldSet()
    {
        var preparedImportDecks = new[]
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
        }.ToList();

        var builder = new DeckImportConfigurationBuilder();

        var result = builder.Build(preparedImportDecks);

        result.Should().ContainSingle();

        var configuration = result.Single();
        configuration.Name.Should().Be("English Words");
        configuration.Notes.Should().HaveCount(2);

        configuration.Notes[0].Id.Should().Be(1);
        configuration.Notes[0].Fields.Should().Equal(new Dictionary<string, string>
        {
            ["FrontField"] = "Hello",
            ["BackField"] = "Hola"
        });

        configuration.Notes[1].Id.Should().Be(2);
        configuration.Notes[1].Fields.Should().Equal(new Dictionary<string, string>
        {
            ["Front"] = "Goodbye",
            ["Back"] = "Adios"
        });
    }

    [Fact]
    public void Build_WhenMultipleNotesHaveSameFieldSet_ReturnsSingleNoteConfiguration()
    {
        var preparedImportDecks = new[]
        {
            PreparedDeck(
                id: 123,
                name: "English Words",
                notes:
                [
                    PreparedNote(1, true),
                    PreparedNote(2, false)
                ])
        }.ToList();

        var builder = new DeckImportConfigurationBuilder();

        var result = builder.Build(preparedImportDecks);

        result.Single().Notes.Should().ContainSingle();
        result.Single().Notes.Single().Fields.Keys.Should().Equal("Front", "Back");
    }

    [Fact]
    public void Build_WhenFieldsAreOutOfDictionaryOrder_OrdersFieldsByAnkiFieldOrder()
    {
        var preparedImportDecks = new[]
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
                            ["Back"] = Field("Hola", 2),
                            ["Front"] = Field("Hello", 1),
                            ["Example"] = Field("Hello there", 3)
                        })
                ])
        }.ToList();

        var builder = new DeckImportConfigurationBuilder();

        var result = builder.Build(preparedImportDecks);

        result.Single().Notes.Single().Fields.Keys
            .Should()
            .Equal("Front", "Back", "Example");
    }

    [Fact]
    public void Build_WhenFieldValuesContainHtmlAndSoundTags_CleansFieldValues()
    {
        var preparedImportDecks = new[]
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
        }.ToList();

        var builder = new DeckImportConfigurationBuilder();

        var result = builder.Build(preparedImportDecks);

        result.Single().Notes.Single().Fields.Should().Equal(new Dictionary<string, string>
        {
            ["Front"] = "dog",
            ["Back"] = "pes"
        });
    }

    [Fact]
    public void Build_WhenDeckHasNoNotes_ReturnsConfigurationWithEmptyNotes()
    {
        var preparedImportDecks = new[]
        {
            PreparedDeck(id: 123, name: "Empty Deck")
        }.ToList();

        var builder = new DeckImportConfigurationBuilder();

        var result = builder.Build(preparedImportDecks);

        result.Should().ContainSingle();
        result.Single().Name.Should().Be("Empty Deck");
        result.Single().Notes.Should().BeEmpty();
    }

    [Fact]
    public void Build_WhenMultipleDecksProvided_ReturnsConfigurationsInInputOrder()
    {
        var preparedImportDecks = new[]
        {
            PreparedDeck(123, "English Words", [PreparedNote(1, true)]),
            PreparedDeck(456, "Spanish Words", [PreparedNote(2, true)])
        }.ToList();

        var builder = new DeckImportConfigurationBuilder();

        var result = builder.Build(preparedImportDecks);

        result.Select(configuration => configuration.Name)
            .Should()
            .Equal("English Words", "Spanish Words");
    }
}
