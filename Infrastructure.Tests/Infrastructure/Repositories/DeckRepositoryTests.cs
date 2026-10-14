using Infrastructure.Tests.Infrastructure.Postgres;
using Application.Services.ConfiguredDecks;
using Application.UseCases.Decks.Settings;
using Domain.Deck;
using FluentAssertions;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using static Infrastructure.Tests.Infrastructure.Postgres.InfrastructureTestData;

namespace Infrastructure.Tests.Infrastructure.Repositories;

[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
public sealed class DeckRepositoryTests(PostgresTestFixture fixture) : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await fixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ReplaceAsync_ReplacesOnlyProvidedUserDecksAndPreservesExistingNoteProgress()
    {

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var preservedReviewDate = today.AddDays(14);

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Decks.AddRange(
                Deck(
                    id: 10,
                    userId: userId,
                    name: "Old deck",
                    dailyNewNotesCount: 7,
                    useOnlyLearnedNotes: false,
                    notes:
                    [
                        Note(
                            id: 100,
                            userId: userId,
                            deckId: 10,
                            learningPoints: 5,
                            nextReviewDate: preservedReviewDate)
                    ]),
                Deck(
                    id: 99,
                    userId: otherUserId,
                    name: "Other user deck",
                    notes:
                    [
                        Note(id: 990, userId: otherUserId, deckId: 99)
                    ]));

            dbContext.DailyNewNoteProgress.Add(new DailyNewNoteProgress
            {
                UserId = userId,
                DeckId = 10,
                Date = today,
                NewAnsweredCount = 4
            });

            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new DeckRepository(dbContext);

            await repository.ReplaceAsync(
                [
                    Deck(
                        id: 10,
                        userId: Guid.Empty,
                        name: "New deck",
                        notes:
                        [
                            Note(id: 100, userId: Guid.Empty, deckId: 0, learningPoints: 0),
                            Note(id: 101, userId: Guid.Empty, deckId: 0)
                        ])
                ],
                userId,
                CancellationToken.None);
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var replacedDeck = await dbContext.Decks
                .Include(deck => deck.Notes)
                .SingleAsync(deck => deck.UserId == userId && deck.Id == 10);

            replacedDeck.Name.Should().Be("New deck");
            replacedDeck.DeckDailyNewNotesCount.Should().Be(7);
            replacedDeck.UseOnlyLearnedNotes.Should().BeFalse();
            replacedDeck.Notes.Should().HaveCount(2);
            replacedDeck.Notes.Should().OnlyContain(note => note.UserId == userId && note.DeckId == 10);

            var preservedNote = replacedDeck.Notes.Single(note => note.Id == 100);
            preservedNote.LearningPoints.Should().Be(5);
            preservedNote.NextReviewDate.Should().Be(preservedReviewDate);

            var progress = await dbContext.DailyNewNoteProgress.SingleAsync();
            progress.NewAnsweredCount.Should().Be(4);

            var otherUserDeck = await dbContext.Decks
                .Include(deck => deck.Notes)
                .SingleAsync(deck => deck.UserId == otherUserId && deck.Id == 99);

            otherUserDeck.Name.Should().Be("Other user deck");
            otherUserDeck.Notes.Should().ContainSingle(note => note.Id == 990);
        }
    }

    [Fact]
    public async Task ReplaceAsync_PreservesOnlyReplacedDeckProgressAndDoesNotDuplicateOtherProgress()
    {

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Decks.AddRange(
                Deck(id: 10, userId: userId, name: "Replaced deck"),
                Deck(id: 11, userId: userId, name: "Not replaced deck"),
                Deck(id: 10, userId: otherUserId, name: "Other user deck"));

            dbContext.DailyNewNoteProgress.AddRange(
                new DailyNewNoteProgress
                {
                    UserId = userId,
                    DeckId = 10,
                    Date = today,
                    NewAnsweredCount = 4
                },
                new DailyNewNoteProgress
                {
                    UserId = userId,
                    DeckId = 11,
                    Date = today,
                    NewAnsweredCount = 7
                },
                new DailyNewNoteProgress
                {
                    UserId = otherUserId,
                    DeckId = 10,
                    Date = today,
                    NewAnsweredCount = 9
                });

            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new DeckRepository(dbContext);

            await repository.ReplaceAsync(
                [
                    Deck(id: 10, userId: Guid.Empty, name: "Imported deck")
                ],
                userId,
                CancellationToken.None);
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var progress = await dbContext.DailyNewNoteProgress
                .OrderBy(item => item.UserId)
                .ThenBy(item => item.DeckId)
                .ToListAsync();

            progress.Should().HaveCount(3);
            progress.Should().ContainSingle(item =>
                item.UserId == userId &&
                item.DeckId == 10 &&
                item.Date == today &&
                item.NewAnsweredCount == 4);
            progress.Should().ContainSingle(item =>
                item.UserId == userId &&
                item.DeckId == 11 &&
                item.Date == today &&
                item.NewAnsweredCount == 7);
            progress.Should().ContainSingle(item =>
                item.UserId == otherUserId &&
                item.DeckId == 10 &&
                item.Date == today &&
                item.NewAnsweredCount == 9);
        }
    }

    [Fact]
    public async Task ReplaceAsync_PreservesExistingNoteProgressOnlyFromCurrentUser()
    {

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var userReviewDate = today.AddDays(2);
        var otherUserReviewDate = today.AddDays(60);

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Decks.AddRange(
                Deck(
                    id: 10,
                    userId: userId,
                    name: "User deck",
                    notes:
                    [
                        Note(
                            id: 100,
                            userId: userId,
                            deckId: 10,
                            learningPoints: 2,
                            nextReviewDate: userReviewDate)
                    ]),
                Deck(
                    id: 10,
                    userId: otherUserId,
                    name: "Other user deck",
                    notes:
                    [
                        Note(
                            id: 100,
                            userId: otherUserId,
                            deckId: 10,
                            learningPoints: 9,
                            nextReviewDate: otherUserReviewDate)
                    ]));

            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new DeckRepository(dbContext);

            await repository.ReplaceAsync(
                [
                    Deck(
                        id: 10,
                        userId: Guid.Empty,
                        name: "Imported deck",
                        notes:
                        [
                            Note(id: 100, userId: Guid.Empty, deckId: 0, learningPoints: 0)
                        ])
                ],
                userId,
                CancellationToken.None);
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var userNote = await dbContext.Notes.SingleAsync(note => note.UserId == userId && note.Id == 100);
            userNote.LearningPoints.Should().Be(2);
            userNote.NextReviewDate.Should().Be(userReviewDate);

            var otherUserNote = await dbContext.Notes.SingleAsync(note => note.UserId == otherUserId && note.Id == 100);
            otherUserNote.LearningPoints.Should().Be(9);
            otherUserNote.NextReviewDate.Should().Be(otherUserReviewDate);
        }
    }

    [Fact]
    public async Task GetDecksStatsAsync_WhenUseOnlyLearnedNotesIsFalse_IncludesUnlearnedNewNotes()
    {

        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Decks.Add(Deck(
                id: 10,
                userId: userId,
                dailyNewNotesCount: 5,
                useOnlyLearnedNotes: false,
                notes:
                [
                    Note(id: 1, userId: userId, deckId: 10, isLearned: true),
                    Note(id: 2, userId: userId, deckId: 10, isLearned: false),
                    Note(id: 3, userId: userId, deckId: 10, isLearned: false),
                    Note(id: 4, userId: userId, deckId: 10, isLearned: true),
                    Note(id: 5, userId: userId, deckId: 10, learningPoints: -1, nextReviewDate: today),
                    Note(id: 6, userId: userId, deckId: 10, learningPoints: 2, nextReviewDate: today)
                ]));

            dbContext.DailyNewNoteProgress.Add(new DailyNewNoteProgress
            {
                UserId = userId,
                DeckId = 10,
                Date = today,
                NewAnsweredCount = 2
            });

            await dbContext.SaveChangesAsync();
        }

        await using var queryContext = fixture.CreateDbContext();
        var repository = new DeckRepository(queryContext);

        var stats = await repository.GetDecksStatsAsync(userId, CancellationToken.None);

        stats.Should().ContainSingle();
        var deckStats = stats.Single();
        deckStats.LearnedNotes.Should().Be(6);
        deckStats.NewNotes.Should().Be(3);
        deckStats.WeakNotes.Should().Be(1);
        deckStats.ReviewNotes.Should().Be(1);
    }

    [Fact]
    public async Task GetDecksStatsAsync_CountsOnlyWeakNotesAvailableForReviewToday()
    {

        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Decks.Add(Deck(
                id: 10,
                userId: userId,
                notes:
                [
                    Note(id: 1, userId: userId, deckId: 10, learningPoints: -1, nextReviewDate: today),
                    Note(id: 2, userId: userId, deckId: 10, learningPoints: -1, nextReviewDate: today.AddDays(-1)),
                    Note(id: 3, userId: userId, deckId: 10, learningPoints: -1, nextReviewDate: today.AddDays(1)),
                    Note(id: 4, userId: userId, deckId: 10, learningPoints: -1, nextReviewDate: DateOnly.MinValue)
                ]));

            await dbContext.SaveChangesAsync();
        }

        await using var queryContext = fixture.CreateDbContext();
        var repository = new DeckRepository(queryContext);

        var stats = await repository.GetDecksStatsAsync(userId, CancellationToken.None);

        stats.Should().ContainSingle();
        stats.Single().WeakNotes.Should().Be(2);
    }

    [Fact]
    public async Task GetDecksStatsAsync_ReturnsOnlyCurrentUserDeckStats()
    {

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Decks.AddRange(
                Deck(
                    id: 10,
                    userId: userId,
                    name: "Current user deck",
                    dailyNewNotesCount: 5,
                    useOnlyLearnedNotes: true,
                    notes:
                    [
                        Note(id: 1, userId: userId, deckId: 10, isLearned: true),
                        Note(id: 2, userId: userId, deckId: 10, isLearned: true, learningPoints: 2, nextReviewDate: today),
                        Note(id: 3, userId: userId, deckId: 10, isLearned: true, learningPoints: -1, nextReviewDate: today)
                    ]),
                Deck(
                    id: 10,
                    userId: otherUserId,
                    name: "Other user same deck id",
                    dailyNewNotesCount: 99,
                    useOnlyLearnedNotes: false,
                    notes:
                    [
                        Note(id: 1, userId: otherUserId, deckId: 10, isLearned: true),
                        Note(id: 2, userId: otherUserId, deckId: 10, isLearned: true),
                        Note(id: 3, userId: otherUserId, deckId: 10, isLearned: true)
                    ]),
                Deck(
                    id: 99,
                    userId: otherUserId,
                    name: "Other user different deck id",
                    dailyNewNotesCount: 99,
                    notes:
                    [
                        Note(id: 99, userId: otherUserId, deckId: 99, isLearned: true)
                    ]));

            dbContext.DailyNewNoteProgress.Add(new DailyNewNoteProgress
            {
                UserId = userId,
                DeckId = 10,
                Date = today,
                NewAnsweredCount = 1
            });

            await dbContext.SaveChangesAsync();
        }

        await using var queryContext = fixture.CreateDbContext();
        var repository = new DeckRepository(queryContext);

        var stats = await repository.GetDecksStatsAsync(userId, CancellationToken.None);

        stats.Should().ContainSingle();
        var deckStats = stats.Single();
        deckStats.DeckId.Should().Be(10);
        deckStats.DeckName.Should().Be("Current user deck");
        deckStats.LearnedNotes.Should().Be(3);
        deckStats.NewNotes.Should().Be(1);
        deckStats.WeakNotes.Should().Be(1);
        deckStats.ReviewNotes.Should().Be(1);
    }

    [Fact]
    public async Task GetDecksStatsAsync_IgnoresOtherUserDailyNewNoteProgressWithSameDeckId()
    {

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Decks.AddRange(
                Deck(
                    id: 10,
                    userId: userId,
                    name: "Current user deck",
                    dailyNewNotesCount: 5,
                    useOnlyLearnedNotes: true,
                    notes:
                    [
                        Note(id: 1, userId: userId, deckId: 10, isLearned: true),
                        Note(id: 2, userId: userId, deckId: 10, isLearned: true)
                    ]),
                Deck(id: 10, userId: otherUserId, name: "Other user same deck id"));

            dbContext.DailyNewNoteProgress.Add(new DailyNewNoteProgress
            {
                UserId = otherUserId,
                DeckId = 10,
                Date = today,
                NewAnsweredCount = 5
            });

            await dbContext.SaveChangesAsync();
        }

        await using var queryContext = fixture.CreateDbContext();
        var repository = new DeckRepository(queryContext);

        var stats = await repository.GetDecksStatsAsync(userId, CancellationToken.None);

        stats.Should().ContainSingle();
        stats.Single().NewNotes.Should().Be(2);
    }

    [Fact]
    public async Task UpdateSettingsAsync_UpdatesOnlyUserDecksAndClampsDailyNewNotesCount()
    {

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Decks.AddRange(
                Deck(id: 10, userId: userId, name: "Old name"),
                Deck(id: 10, userId: otherUserId, name: "Other user name"));

            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new DeckRepository(dbContext);

            await repository.UpdateSettingsAsync(
                new DeckSettings
                {
                    NewDeckNames = new Dictionary<long, string> { [10] = "New name" },
                    DeckDailyNewNotesCount = new Dictionary<long, int> { [10] = -3 },
                    UseOnlyLearnedNotes = new Dictionary<long, bool> { [10] = false }
                },
                userId,
                CancellationToken.None);
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var userDeck = await dbContext.Decks.SingleAsync(deck => deck.UserId == userId && deck.Id == 10);
            userDeck.Name.Should().Be("New name");
            userDeck.DeckDailyNewNotesCount.Should().Be(0);
            userDeck.UseOnlyLearnedNotes.Should().BeFalse();

            var otherUserDeck = await dbContext.Decks.SingleAsync(deck => deck.UserId == otherUserId && deck.Id == 10);
            otherUserDeck.Name.Should().Be("Other user name");
            otherUserDeck.UseOnlyLearnedNotes.Should().BeTrue();
        }
    }

    [Fact]
    public async Task ReplaceAllAsync_ReplacesAllUserDecksButLeavesOtherUsersDecks()
    {

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var preservedReviewDate = today.AddDays(21);

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Decks.AddRange(
                Deck(
                    id: 10,
                    userId: userId,
                    name: "Old first deck",
                    dailyNewNotesCount: 6,
                    useOnlyLearnedNotes: false,
                    notes:
                    [
                        Note(id: 100, userId: userId, deckId: 10, learningPoints: 4, nextReviewDate: preservedReviewDate)
                    ]),
                Deck(id: 11, userId: userId, name: "Old second deck"),
                Deck(id: 10, userId: otherUserId, name: "Other user deck"));

            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new DeckRepository(dbContext);

            await repository.ReplaceAllAsync(
                [
                    Deck(
                        id: 10,
                        userId: Guid.Empty,
                        name: "Imported first deck",
                        notes:
                        [
                            Note(id: 100, userId: Guid.Empty, deckId: 0),
                            Note(id: 101, userId: Guid.Empty, deckId: 0)
                        ])
                ],
                userId,
                CancellationToken.None);
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var userDecks = await dbContext.Decks
                .Include(deck => deck.Notes)
                .Where(deck => deck.UserId == userId)
                .ToListAsync();

            userDecks.Should().ContainSingle(deck => deck.Id == 10);
            userDecks.Should().NotContain(deck => deck.Id == 11);

            var replacedDeck = userDecks.Single();
            replacedDeck.Name.Should().Be("Imported first deck");
            replacedDeck.DeckDailyNewNotesCount.Should().Be(6);
            replacedDeck.UseOnlyLearnedNotes.Should().BeFalse();

            var preservedNote = replacedDeck.Notes.Single(note => note.Id == 100);
            preservedNote.LearningPoints.Should().Be(4);
            preservedNote.NextReviewDate.Should().Be(preservedReviewDate);

            var otherUserDeck = await dbContext.Decks.SingleAsync(deck => deck.UserId == otherUserId && deck.Id == 10);
            otherUserDeck.Name.Should().Be("Other user deck");
        }
    }

    [Fact]
    public async Task SaveDeckImportSettingsAsync_ReplacesUserSettingsAndDeduplicatesMappings()
    {

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.DeckImportSettings.AddRange(
                new DeckImportSettings
                {
                    UserId = userId,
                    DeckId = 10,
                    DeckName = "Old user settings",
                    FieldMappings =
                    [
                        new DeckImportFieldMapping
                        {
                            UserId = userId,
                            DeckId = 10,
                            FrontKey = "OldFront",
                            BackKey = "OldBack"
                        }
                    ]
                },
                new DeckImportSettings
                {
                    UserId = otherUserId,
                    DeckId = 10,
                    DeckName = "Other user settings",
                    FieldMappings =
                    [
                        new DeckImportFieldMapping
                        {
                            UserId = otherUserId,
                            DeckId = 10,
                            FrontKey = "OtherFront",
                            BackKey = "OtherBack"
                        }
                    ]
                });

            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new DeckRepository(dbContext);

            await repository.SaveDeckImportSettingsAsync(
                [
                    new DeckImportSelection
                    {
                        DeckId = 10,
                        DeckName = "New user settings",
                        FieldMappings =
                        [
                            new DeckImportFieldSelection { FrontKey = "Front", BackKey = "Back" },
                            new DeckImportFieldSelection { FrontKey = "Front", BackKey = "Back" },
                            new DeckImportFieldSelection { FrontKey = "Word", BackKey = "Translation" }
                        ]
                    },
                    new DeckImportSelection
                    {
                        DeckId = 10,
                        DeckName = "Duplicate deck should be ignored",
                        FieldMappings =
                        [
                            new DeckImportFieldSelection { FrontKey = "IgnoredFront", BackKey = "IgnoredBack" }
                        ]
                    }
                ],
                userId,
                CancellationToken.None);
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var userSettings = await dbContext.DeckImportSettings
                .Include(settings => settings.FieldMappings)
                .SingleAsync(settings => settings.UserId == userId && settings.DeckId == 10);

            userSettings.DeckName.Should().Be("New user settings");
            userSettings.FieldMappings.Should().HaveCount(2);
            userSettings.FieldMappings.Should().Contain(mapping => mapping.FrontKey == "Front" && mapping.BackKey == "Back");
            userSettings.FieldMappings.Should().Contain(mapping => mapping.FrontKey == "Word" && mapping.BackKey == "Translation");

            var otherUserSettings = await dbContext.DeckImportSettings
                .Include(settings => settings.FieldMappings)
                .SingleAsync(settings => settings.UserId == otherUserId && settings.DeckId == 10);

            otherUserSettings.DeckName.Should().Be("Other user settings");
            otherUserSettings.FieldMappings.Should().ContainSingle();
        }
    }

    [Fact]
    public async Task GetDeckImportSettingsAsync_ReturnsOnlyRequestedCurrentUserSettingsWithMappings()
    {

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.DeckImportSettings.AddRange(
                new DeckImportSettings
                {
                    UserId = userId,
                    DeckId = 10,
                    DeckName = "Requested user deck",
                    FieldMappings =
                    [
                        new DeckImportFieldMapping
                        {
                            UserId = userId,
                            DeckId = 10,
                            FrontKey = "Front",
                            BackKey = "Back"
                        },
                        new DeckImportFieldMapping
                        {
                            UserId = userId,
                            DeckId = 10,
                            FrontKey = "Word",
                            BackKey = "Translation"
                        }
                    ]
                },
                new DeckImportSettings
                {
                    UserId = userId,
                    DeckId = 11,
                    DeckName = "Not requested user deck",
                    FieldMappings =
                    [
                        new DeckImportFieldMapping
                        {
                            UserId = userId,
                            DeckId = 11,
                            FrontKey = "IgnoredFront",
                            BackKey = "IgnoredBack"
                        }
                    ]
                },
                new DeckImportSettings
                {
                    UserId = otherUserId,
                    DeckId = 10,
                    DeckName = "Other user same deck id",
                    FieldMappings =
                    [
                        new DeckImportFieldMapping
                        {
                            UserId = otherUserId,
                            DeckId = 10,
                            FrontKey = "OtherFront",
                            BackKey = "OtherBack"
                        }
                    ]
                });

            await dbContext.SaveChangesAsync();
        }

        await using var queryContext = fixture.CreateDbContext();
        var repository = new DeckRepository(queryContext);

        var settings = await repository.GetDeckImportSettingsAsync([10], userId, CancellationToken.None);

        settings.Should().ContainSingle();
        var requestedSettings = settings.Single();
        requestedSettings.UserId.Should().Be(userId);
        requestedSettings.DeckId.Should().Be(10);
        requestedSettings.DeckName.Should().Be("Requested user deck");
        requestedSettings.FieldMappings.Should().HaveCount(2);
        requestedSettings.FieldMappings.Should().OnlyContain(mapping =>
            mapping.UserId == userId &&
            mapping.DeckId == 10);
        requestedSettings.FieldMappings.Should().Contain(mapping =>
            mapping.FrontKey == "Front" &&
            mapping.BackKey == "Back");
        requestedSettings.FieldMappings.Should().Contain(mapping =>
            mapping.FrontKey == "Word" &&
            mapping.BackKey == "Translation");
    }

    [Fact]
    public async Task DeleteDecksAsync_DeletesOnlySelectedUserDeckData()
    {

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Decks.AddRange(
                Deck(id: 10, userId: userId, name: "Delete me"),
                Deck(id: 11, userId: userId, name: "Keep me"),
                Deck(id: 10, userId: otherUserId, name: "Other user keep"));

            dbContext.DailyNewNoteProgress.AddRange(
                new DailyNewNoteProgress { UserId = userId, DeckId = 10, Date = today, NewAnsweredCount = 2 },
                new DailyNewNoteProgress { UserId = userId, DeckId = 11, Date = today, NewAnsweredCount = 3 },
                new DailyNewNoteProgress { UserId = otherUserId, DeckId = 10, Date = today, NewAnsweredCount = 4 });

            dbContext.DeckImportSettings.AddRange(
                new DeckImportSettings { UserId = userId, DeckId = 10, DeckName = "Delete settings" },
                new DeckImportSettings { UserId = userId, DeckId = 11, DeckName = "Keep settings" },
                new DeckImportSettings { UserId = otherUserId, DeckId = 10, DeckName = "Other keep settings" });

            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new DeckRepository(dbContext);

            await repository.DeleteDecksAsync([10], userId, CancellationToken.None);
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var decks = await dbContext.Decks.Select(deck => new { deck.UserId, deck.Id }).ToListAsync();
            decks.Should().NotContain(deck => deck.UserId == userId && deck.Id == 10);
            decks.Should().Contain(deck => deck.UserId == userId && deck.Id == 11);
            decks.Should().Contain(deck => deck.UserId == otherUserId && deck.Id == 10);

            var progress = await dbContext.DailyNewNoteProgress.Select(x => new { x.UserId, x.DeckId }).ToListAsync();
            progress.Should().NotContain(x => x.UserId == userId && x.DeckId == 10);
            progress.Should().Contain(x => x.UserId == userId && x.DeckId == 11);
            progress.Should().Contain(x => x.UserId == otherUserId && x.DeckId == 10);

            var settings = await dbContext.DeckImportSettings.Select(x => new { x.UserId, x.DeckId }).ToListAsync();
            settings.Should().NotContain(x => x.UserId == userId && x.DeckId == 10);
            settings.Should().Contain(x => x.UserId == userId && x.DeckId == 11);
            settings.Should().Contain(x => x.UserId == otherUserId && x.DeckId == 10);
        }
    }

    [Fact]
    public async Task GetSettingsStateAsync_ReturnsOnlyCurrentUserSettings()
    {

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Decks.AddRange(
                Deck(id: 10, userId: userId, name: "First", dailyNewNotesCount: 12, useOnlyLearnedNotes: true),
                Deck(id: 11, userId: userId, name: "Second", dailyNewNotesCount: 7, useOnlyLearnedNotes: false),
                Deck(id: 99, userId: otherUserId, name: "Other", dailyNewNotesCount: 99, useOnlyLearnedNotes: false));

            await dbContext.SaveChangesAsync();
        }

        await using var queryContext = fixture.CreateDbContext();
        var repository = new DeckRepository(queryContext);

        var settings = await repository.GetSettingsStateAsync(userId, CancellationToken.None);

        settings.NewDeckNames.Should().BeEquivalentTo(new Dictionary<long, string>
        {
            [10] = "First",
            [11] = "Second"
        });
        settings.DeckDailyNewNotesCount.Should().BeEquivalentTo(new Dictionary<long, int>
        {
            [10] = 12,
            [11] = 7
        });
        settings.UseOnlyLearnedNotes.Should().BeEquivalentTo(new Dictionary<long, bool>
        {
            [10] = true,
            [11] = false
        });
    }

    [Fact]
    public async Task GetDeckLanguage_WhenDeckDoesNotBelongToUser_ThrowsKeyNotFoundException()
    {

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Decks.Add(Deck(id: 10, userId: otherUserId));
            await dbContext.SaveChangesAsync();
        }

        await using var queryContext = fixture.CreateDbContext();
        var repository = new DeckRepository(queryContext);

        var act = () => repository.GetDeckLanguage(10, userId, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Deck with id '10' was not found.");
    }

    [Fact]
    public async Task GetAllDeckNamesAndIdsAsync_ReturnsOnlyCurrentUserDeckNamesAndIds()
    {

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Decks.AddRange(
                Deck(id: 10, userId: userId, name: "English"),
                Deck(id: 11, userId: userId, name: "Polish"),
                Deck(id: 99, userId: otherUserId, name: "Other user deck"));

            await dbContext.SaveChangesAsync();
        }

        await using var queryContext = fixture.CreateDbContext();
        var repository = new DeckRepository(queryContext);

        var deckNamesAndIds = await repository.GetAllDeckNamesAndIdsAsync(userId, CancellationToken.None);

        deckNamesAndIds.Should().BeEquivalentTo(new Dictionary<string, long>
        {
            ["English"] = 10,
            ["Polish"] = 11
        });
        deckNamesAndIds.Should().NotContainKey("Other user deck");
    }
}
