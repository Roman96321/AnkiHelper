using Application.Abstractions.OpenAI;
using Application.Tests.TestData;
using Application.UseCases.Training.SentenceComposition;
using Domain.Deck;
using FluentAssertions;
using NSubstitute;
using static Application.Tests.TestData.TrainingRepositoryTestHelper;

namespace Application.Tests.UseCases.Training.SentenceComposition;

public class SentenceCompositionUseCaseTests
{
    private readonly TrainingRepositoryTestHelper _training = new();
    private readonly ISentenceCompositionCheckAnswer _checkAnswer = Substitute.For<ISentenceCompositionCheckAnswer>();

    [Fact]
    public async Task GetQuestionAsync_WhenNoAnswerInRequest_ReturnsWordsFromNewNoteAndRandomNotes()
    {
        var userId = Guid.NewGuid();
        var request = new SentenceCompositionRequest { DeckId = 10 };
        var targetNote = Note(id: 1, deckId: request.DeckId, userId: userId, front: "dog");
        var randomNotes = new List<Note>
        {
            Note(id: 2, deckId: request.DeckId, userId: userId, front: "cat"),
            Note(id: 3, deckId: request.DeckId, userId: userId, front: "house")
        };

        _training.ReturnsNewNote(request.DeckId, userId, targetNote);
        _training.ReturnsRandomNotes(
            request.DeckId,
            targetNote.Id,
            count => count >= 1 && count < 3,
            userId,
            randomNotes);
        _training.ReturnsRemainingCount(request.DeckId, userId, 12);

        var useCase = CreateUseCase();

        var result = await useCase.GetQuestionAsync(request, userId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.NoteId.Should().Be(targetNote.Id);
        result.Words.Should().Equal("dog", "cat", "house");
        result.RemainingCount.Should().Be(12);

        await _checkAnswer.DidNotReceiveWithAnyArgs()
            .CheckAnswer(default!, default);

        await _training.Repository.DidNotReceiveWithAnyArgs()
            .UpdateNote(default, default, default, default, default);
    }

    [Fact]
    public async Task GetQuestionAsync_WhenAnswerIsInRequest_ChecksAnswerAndUpdatesAnsweredNote()
    {
        var userId = Guid.NewGuid();
        var request = new SentenceCompositionRequest
        {
            DeckId = 10,
            NoteId = 99,
            AnswerSentence = "The dog sleeps",
            Words = ["dog", "sleep"]
        };
        var answerResult = new SentenceCompositionResponse
        {
            IsCorrect = true,
            Verdict = "Correct",
            MoreCorrectSentence = "The dog sleeps"
        };
        var nextNote = Note(id: 1, deckId: request.DeckId, userId: userId, front: "cat");
        var randomNotes = new List<Note> { Note(id: 2, deckId: request.DeckId, userId: userId, front: "house") };

        _checkAnswer.CheckAnswer(
                Arg.Is<SentenceCompositionRequest>(actualRequest =>
                    actualRequest == request &&
                    actualRequest.UserId == userId),
                Arg.Any<CancellationToken>())
            .Returns(answerResult);

        _training.ReturnsNewNote(request.DeckId, userId, nextNote);
        _training.ReturnsRandomNotes(
            request.DeckId,
            nextNote.Id,
            count => count >= 1 && count < 3,
            userId,
            randomNotes);
        _training.ReturnsRemainingCount(request.DeckId, userId, 6);

        var useCase = CreateUseCase();

        var result = await useCase.GetQuestionAsync(request, userId, CancellationToken.None);

        result.Should().BeSameAs(answerResult);
        result!.IsCorrect.Should().BeTrue();
        result.NoteId.Should().Be(nextNote.Id);
        result.Words.Should().Equal("cat", "house");
        result.RemainingCount.Should().Be(6);

        await _training.Repository.Received(1)
            .UpdateNote(request.DeckId, request.NoteId, answerResult.IsCorrect, userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetQuestionAsync_WhenNoNewNoteExists_ReturnsWordsFromReviewOrWeakNote()
    {
        var userId = Guid.NewGuid();
        var request = new SentenceCompositionRequest { DeckId = 10 };
        var reviewNote = Note(id: 7, deckId: request.DeckId, userId: userId, front: "dog");
        var randomNotes = new List<Note> { Note(id: 8, deckId: request.DeckId, userId: userId, front: "cat") };

        _training.ReturnsNewNote(request.DeckId, userId, null);
        _training.ReturnsReviewOrWeakNote(request.DeckId, userId, reviewNote);
        _training.ReturnsRandomNotes(
            request.DeckId,
            reviewNote.Id,
            count => count >= 1 && count < 3,
            userId,
            randomNotes);
        _training.ReturnsRemainingCount(request.DeckId, userId, 3);

        var useCase = CreateUseCase();

        var result = await useCase.GetQuestionAsync(request, userId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.NoteId.Should().Be(reviewNote.Id);
        result.Words.Should().Equal("dog", "cat");

        await _training.Repository.Received(1)
            .GetReviewAndWeakNoteAsync(request.DeckId, userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetQuestionAsync_WhenNoAnswerAndNoTrainingNotesExist_ReturnsNull()
    {
        var userId = Guid.NewGuid();
        var request = new SentenceCompositionRequest { DeckId = 10 };

        _training.ReturnsNewNote(request.DeckId, userId, null);
        _training.ReturnsReviewOrWeakNote(request.DeckId, userId, null);

        var useCase = CreateUseCase();

        var result = await useCase.GetQuestionAsync(request, userId, CancellationToken.None);

        result.Should().BeNull();

        await _checkAnswer.DidNotReceiveWithAnyArgs()
            .CheckAnswer(default!, default);

        await _training.Repository.DidNotReceiveWithAnyArgs()
            .GetRandomNotesAsync(default, default, default, default, default);
    }

    [Fact]
    public async Task GetQuestionAsync_WhenAnswerWasCheckedButNoNextTrainingNotesExist_ReturnsVerdictWithZeroRemaining()
    {
        var userId = Guid.NewGuid();
        var request = new SentenceCompositionRequest
        {
            DeckId = 10,
            NoteId = 99,
            AnswerSentence = "bad answer"
        };
        var answerResult = new SentenceCompositionResponse
        {
            IsCorrect = false,
            Verdict = "Not quite",
            MoreCorrectSentence = "Better answer"
        };

        _checkAnswer.CheckAnswer(request, Arg.Any<CancellationToken>())
            .Returns(answerResult);

        _training.ReturnsNewNote(request.DeckId, userId, null);
        _training.ReturnsReviewOrWeakNote(request.DeckId, userId, null);

        var useCase = CreateUseCase();

        var result = await useCase.GetQuestionAsync(request, userId, CancellationToken.None);

        result.Should().BeSameAs(answerResult);
        result!.RemainingCount.Should().Be(0);
        result.Verdict.Should().Be("Not quite");

        await _training.Repository.Received(1)
            .UpdateNote(request.DeckId, request.NoteId, answerResult.IsCorrect, userId, Arg.Any<CancellationToken>());

        await _training.Repository.DidNotReceiveWithAnyArgs()
            .GetRandomNotesAsync(default, default, default, default, default);
    }

    private SentenceCompositionUseCase CreateUseCase()
    {
        return new SentenceCompositionUseCase(_checkAnswer, _training.Repository);
    }
}
