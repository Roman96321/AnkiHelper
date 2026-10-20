using Infrastructure.Tests.Infrastructure.SQLServer;
using Domain.Deck;
using FluentAssertions;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using static Infrastructure.Tests.Infrastructure.SQLServer.InfrastructureTestData;

namespace Infrastructure.Tests.Infrastructure.Repositories;

[Collection(SQLServerCollection.Name)]
[Trait("Category", "Integration")]
public sealed class TrainingRepositoryTests(SQLServerTestFixture fixture) : IAsyncLifetime
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
    public async Task GetNewNoteAsync_WhenDailyLimitIsReached_ReturnsNull()
    {

        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Decks.Add(Deck(
                id: 10,
                userId: userId,
                dailyNewNotesCount: 1,
                notes:
                [
                    Note(id: 1, userId: userId, deckId: 10)
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
        var repository = new TrainingRepository(queryContext);

        var note = await repository.GetNewNoteAsync(10, userId, CancellationToken.None);

        note.Should().BeNull();
    }

    [Fact]
    public async Task GetRemainingNotesCountAsync_RespectsDailyNewNoteLimitAndIncludesReviewNotes()
    {

        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Decks.Add(Deck(
                id: 10,
                userId: userId,
                dailyNewNotesCount: 3,
                useOnlyLearnedNotes: false,
                notes:
                [
                    Note(id: 1, userId: userId, deckId: 10, isLearned: true),
                    Note(id: 2, userId: userId, deckId: 10, isLearned: false),
                    Note(id: 3, userId: userId, deckId: 10, isLearned: false),
                    Note(id: 4, userId: userId, deckId: 10, learningPoints: 2, nextReviewDate: today),
                    Note(id: 5, userId: userId, deckId: 10, learningPoints: 2, nextReviewDate: today.AddDays(1))
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
        var repository = new TrainingRepository(queryContext);

        var remaining = await repository.GetRemainingNotesCountAsync(10, userId, CancellationToken.None);

        remaining.Should().Be(3);
    }

    [Fact]
    public async Task UpdateNote_WhenCorrectAnswerForNewNote_UpdatesProgressAndDailyStatistics()
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
                    Note(id: 1, userId: userId, deckId: 10, learningPoints: 0, nextReviewDate: DateOnly.MinValue)
                ]));

            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new TrainingRepository(dbContext);

            await repository.UpdateNote(10, 1, isCorrectAnswer: true, userId, CancellationToken.None);
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var note = await dbContext.Notes.SingleAsync(note => note.UserId == userId && note.Id == 1);
            note.LearningPoints.Should().Be(1);
            note.NextReviewDate.Should().Be(today.AddDays(2));

            var newNoteProgress = await dbContext.DailyNewNoteProgress.SingleAsync();
            newNoteProgress.NewAnsweredCount.Should().Be(1);

            var trainingStatistics = await dbContext.DailyTrainingStatistics.SingleAsync();
            trainingStatistics.LearningNoteCounts.Should().Be(1);
        }
    }

    [Fact]
    public async Task UpdateNote_WhenIncorrectAnswerWithHighLearningPoints_HalvesLearningPoints()
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
                    Note(id: 1, userId: userId, deckId: 10, learningPoints: 6, nextReviewDate: today)
                ]));

            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new TrainingRepository(dbContext);

            await repository.UpdateNote(10, 1, isCorrectAnswer: false, userId, CancellationToken.None);
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var note = await dbContext.Notes.SingleAsync(note => note.UserId == userId && note.Id == 1);
            note.LearningPoints.Should().Be(3);
            note.NextReviewDate.Should().Be(today.AddDays(7));

            var hasTrainingStatistics = await dbContext.DailyTrainingStatistics.AnyAsync();
            hasTrainingStatistics.Should().BeFalse();
        }
    }

    [Fact]
    public async Task GetNewNoteAsync_WhenUseOnlyLearnedNotesIsTrue_IgnoresUnlearnedNotes()
    {

        var userId = Guid.NewGuid();

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Decks.Add(Deck(
                id: 10,
                userId: userId,
                useOnlyLearnedNotes: true,
                notes:
                [
                    Note(id: 1, userId: userId, deckId: 10, isLearned: false),
                    Note(id: 2, userId: userId, deckId: 10, isLearned: true)
                ]));

            await dbContext.SaveChangesAsync();
        }

        await using var queryContext = fixture.CreateDbContext();
        var repository = new TrainingRepository(queryContext);

        var note = await repository.GetNewNoteAsync(10, userId, CancellationToken.None);

        note.Should().NotBeNull();
        note!.Id.Should().Be(2);
    }

    [Fact]
    public async Task GetNewNoteAsync_WhenUseOnlyLearnedNotesIsFalse_AllowsUnlearnedNotes()
    {

        var userId = Guid.NewGuid();

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Decks.Add(Deck(
                id: 10,
                userId: userId,
                useOnlyLearnedNotes: false,
                notes:
                [
                    Note(id: 1, userId: userId, deckId: 10, isLearned: false)
                ]));

            await dbContext.SaveChangesAsync();
        }

        await using var queryContext = fixture.CreateDbContext();
        var repository = new TrainingRepository(queryContext);

        var note = await repository.GetNewNoteAsync(10, userId, CancellationToken.None);

        note.Should().NotBeNull();
        note!.Id.Should().Be(1);
        note.IsLearned.Should().BeFalse();
    }

    [Fact]
    public async Task GetRandomNotesAsync_ReturnsOnlyLearnedNotesFromCurrentUserDeckAndExcludesTargetNote()
    {

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Decks.AddRange(
                Deck(
                    id: 10,
                    userId: userId,
                    notes:
                    [
                        Note(id: 1, userId: userId, deckId: 10, isLearned: true),
                        Note(id: 2, userId: userId, deckId: 10, isLearned: true),
                        Note(id: 3, userId: userId, deckId: 10, isLearned: false)
                    ]),
                Deck(
                    id: 10,
                    userId: otherUserId,
                    notes:
                    [
                        Note(id: 1, userId: otherUserId, deckId: 10, isLearned: true),
                        Note(id: 4, userId: otherUserId, deckId: 10, isLearned: true)
                    ]));

            await dbContext.SaveChangesAsync();
        }

        await using var queryContext = fixture.CreateDbContext();
        var repository = new TrainingRepository(queryContext);

        var notes = await repository.GetRandomNotesAsync(10, excludedNoteId: 1, count: 10, userId, CancellationToken.None);

        notes.Should().ContainSingle();
        notes.Single().Id.Should().Be(2);
        notes.Single().UserId.Should().Be(userId);
    }

    [Fact]
    public async Task GetReviewAndWeakNoteAsync_ReturnsOnlyDueReviewOrWeakNote()
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
                    Note(id: 1, userId: userId, deckId: 10, nextReviewDate: DateOnly.MinValue),
                    Note(id: 2, userId: userId, deckId: 10, learningPoints: 2, nextReviewDate: today.AddDays(1)),
                    Note(id: 3, userId: userId, deckId: 10, learningPoints: -1, nextReviewDate: today)
                ]));

            await dbContext.SaveChangesAsync();
        }

        await using var queryContext = fixture.CreateDbContext();
        var repository = new TrainingRepository(queryContext);

        var note = await repository.GetReviewAndWeakNoteAsync(10, userId, CancellationToken.None);

        note.Should().NotBeNull();
        note!.Id.Should().Be(3);
    }

    [Fact]
    public async Task UpdateNote_WhenIncorrectAnswerWouldReachMinusFour_CapsLearningPointsAtMinusOne()
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
                    Note(id: 1, userId: userId, deckId: 10, learningPoints: -3, nextReviewDate: today)
                ]));

            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new TrainingRepository(dbContext);

            await repository.UpdateNote(10, 1, isCorrectAnswer: false, userId, CancellationToken.None);
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var note = await dbContext.Notes.SingleAsync(note => note.UserId == userId && note.Id == 1);
            note.LearningPoints.Should().Be(-1);
            note.NextReviewDate.Should().Be(today);
        }
    }

    [Fact]
    public async Task GetDailyTrainingStatisticsAsync_ReturnsOnlyCurrentUserStatistics()
    {

        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var yesterday = today.AddDays(-1);

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.DailyTrainingStatistics.AddRange(
                new DailyTrainingStatistics { UserId = userId, Date = today, LearningNoteCounts = 5 },
                new DailyTrainingStatistics { UserId = userId, Date = yesterday, LearningNoteCounts = 2 },
                new DailyTrainingStatistics { UserId = otherUserId, Date = today, LearningNoteCounts = 99 });

            await dbContext.SaveChangesAsync();
        }

        await using var queryContext = fixture.CreateDbContext();
        var repository = new TrainingRepository(queryContext);

        var statistics = await repository.GetDailyTrainingStatisticsAsync(userId, CancellationToken.None);

        statistics.Should().BeEquivalentTo(new Dictionary<DateOnly, int>
        {
            [today] = 5,
            [yesterday] = 2
        });
    }

    [Fact]
    public async Task GetNewNoteAsync_ReturnsNullWhenOnlyOtherUserHasEligibleNewNote()
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
                    notes:
                    [
                        Note(id: 1, userId: userId, deckId: 10, nextReviewDate: today.AddDays(1))
                    ]),
                Deck(
                    id: 10,
                    userId: otherUserId,
                    notes:
                    [
                        Note(id: 1, userId: otherUserId, deckId: 10, nextReviewDate: DateOnly.MinValue)
                    ]));

            await dbContext.SaveChangesAsync();
        }

        await using var queryContext = fixture.CreateDbContext();
        var repository = new TrainingRepository(queryContext);

        var note = await repository.GetNewNoteAsync(10, userId, CancellationToken.None);

        note.Should().BeNull();
    }

    [Fact]
    public async Task GetNewNoteAsync_IgnoresOtherUserDailyNewNoteProgressWithSameDeckId()
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
                    dailyNewNotesCount: 5,
                    notes:
                    [
                        Note(id: 1, userId: userId, deckId: 10, nextReviewDate: DateOnly.MinValue)
                    ]),
                Deck(id: 10, userId: otherUserId, dailyNewNotesCount: 5));

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
        var repository = new TrainingRepository(queryContext);

        var note = await repository.GetNewNoteAsync(10, userId, CancellationToken.None);

        note.Should().NotBeNull();
        note!.UserId.Should().Be(userId);
        note.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetReviewAndWeakNoteAsync_ReturnsNullWhenOnlyOtherUserHasDueNote()
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
                    notes:
                    [
                        Note(id: 1, userId: userId, deckId: 10, nextReviewDate: today.AddDays(1))
                    ]),
                Deck(
                    id: 10,
                    userId: otherUserId,
                    notes:
                    [
                        Note(id: 1, userId: otherUserId, deckId: 10, learningPoints: 2, nextReviewDate: today)
                    ]));

            await dbContext.SaveChangesAsync();
        }

        await using var queryContext = fixture.CreateDbContext();
        var repository = new TrainingRepository(queryContext);

        var note = await repository.GetReviewAndWeakNoteAsync(10, userId, CancellationToken.None);

        note.Should().BeNull();
    }

    [Fact]
    public async Task GetRemainingNotesCountAsync_IgnoresOtherUserNotesWithSameDeckId()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Decks.AddRange(
                Deck(id: 10, userId: userId, dailyNewNotesCount: 5, useOnlyLearnedNotes: false),
                Deck(
                    id: 10,
                    userId: otherUserId,
                    dailyNewNotesCount: 5,
                    useOnlyLearnedNotes: false,
                    notes:
                    [
                        Note(id: 1, userId: otherUserId, deckId: 10, nextReviewDate: DateOnly.MinValue),
                        Note(id: 2, userId: otherUserId, deckId: 10, learningPoints: 2, nextReviewDate: today)
                    ]));

            await dbContext.SaveChangesAsync();
        }

        await using var queryContext = fixture.CreateDbContext();
        var repository = new TrainingRepository(queryContext);

        var remainingCount = await repository.GetRemainingNotesCountAsync(10, userId, CancellationToken.None);

        remainingCount.Should().Be(0);
    }

    [Fact]
    public async Task GetRemainingNotesCountAsync_IgnoresOtherUserDailyNewNoteProgressWithSameDeckId()
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
                    dailyNewNotesCount: 5,
                    useOnlyLearnedNotes: true,
                    notes:
                    [
                        Note(id: 1, userId: userId, deckId: 10, isLearned: true, nextReviewDate: DateOnly.MinValue),
                        Note(id: 2, userId: userId, deckId: 10, isLearned: true, nextReviewDate: DateOnly.MinValue)
                    ]),
                Deck(id: 10, userId: otherUserId, dailyNewNotesCount: 5));

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
        var repository = new TrainingRepository(queryContext);

        var remainingCount = await repository.GetRemainingNotesCountAsync(10, userId, CancellationToken.None);

        remainingCount.Should().Be(2);
    }

    [Fact]
    public async Task UpdateNote_WhenNoteBelongsOnlyToOtherUser_ThrowsAndDoesNotUpdateOtherUserNote()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Decks.AddRange(
                Deck(id: 10, userId: userId),
                Deck(
                    id: 10,
                    userId: otherUserId,
                    notes:
                    [
                        Note(id: 1, userId: otherUserId, deckId: 10, learningPoints: 6, nextReviewDate: today)
                    ]));

            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new TrainingRepository(dbContext);

            var act = () => repository.UpdateNote(10, 1, isCorrectAnswer: true, userId, CancellationToken.None);

            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var otherUserNote = await dbContext.Notes.SingleAsync(note => note.UserId == otherUserId && note.Id == 1);
            otherUserNote.LearningPoints.Should().Be(6);
            otherUserNote.NextReviewDate.Should().Be(today);

            var currentUserProgressExists = await dbContext.DailyNewNoteProgress
                .AnyAsync(progress => progress.UserId == userId);
            currentUserProgressExists.Should().BeFalse();

            var currentUserTrainingStatsExists = await dbContext.DailyTrainingStatistics
                .AnyAsync(statistics => statistics.UserId == userId);
            currentUserTrainingStatsExists.Should().BeFalse();
        }
    }

    [Fact]
    public async Task UpdateNote_WhenCorrectAnswer_IgnoresOtherUserDailyTrainingStatistics()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.Today);

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Decks.Add(Deck(
                id: 10,
                userId: userId,
                notes:
                [
                    Note(id: 1, userId: userId, deckId: 10, learningPoints: 0, nextReviewDate: today)
                ]));

            dbContext.DailyTrainingStatistics.Add(new DailyTrainingStatistics
            {
                UserId = otherUserId,
                Date = today,
                LearningNoteCounts = 7
            });

            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new TrainingRepository(dbContext);

            await repository.UpdateNote(10, 1, isCorrectAnswer: true, userId, CancellationToken.None);
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var currentUserStatistics = await dbContext.DailyTrainingStatistics
                .SingleAsync(statistics => statistics.UserId == userId && statistics.Date == today);
            currentUserStatistics.LearningNoteCounts.Should().Be(1);

            var otherUserStatistics = await dbContext.DailyTrainingStatistics
                .SingleAsync(statistics => statistics.UserId == otherUserId && statistics.Date == today);
            otherUserStatistics.LearningNoteCounts.Should().Be(7);
        }
    }

    [Fact]
    public async Task UpdateNote_WhenNewNote_IgnoresOtherUserDailyNewNoteProgress()
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
                    notes:
                    [
                        Note(id: 1, userId: userId, deckId: 10, learningPoints: 0, nextReviewDate: DateOnly.MinValue)
                    ]),
                Deck(id: 10, userId: otherUserId));

            dbContext.DailyNewNoteProgress.Add(new DailyNewNoteProgress
            {
                UserId = otherUserId,
                DeckId = 10,
                Date = today,
                NewAnsweredCount = 7
            });

            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new TrainingRepository(dbContext);

            await repository.UpdateNote(10, 1, isCorrectAnswer: true, userId, CancellationToken.None);
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var currentUserProgress = await dbContext.DailyNewNoteProgress
                .SingleAsync(progress => progress.UserId == userId && progress.DeckId == 10 && progress.Date == today);
            currentUserProgress.NewAnsweredCount.Should().Be(1);

            var otherUserProgress = await dbContext.DailyNewNoteProgress
                .SingleAsync(progress => progress.UserId == otherUserId && progress.DeckId == 10 && progress.Date == today);
            otherUserProgress.NewAnsweredCount.Should().Be(7);
        }
    }
}
