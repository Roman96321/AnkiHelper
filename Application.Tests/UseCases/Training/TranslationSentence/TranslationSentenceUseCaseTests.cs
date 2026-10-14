using Application.Abstractions.OpenAI;
using Application.Tests.TestData;
using Application.UseCases.Training.TranslationSentence;
using Domain.Deck;
using FluentAssertions;
using NSubstitute;
using static Application.Tests.TestData.TrainingRepositoryTestHelper;

namespace Application.Tests.UseCases.Training.TranslationSentence;

public class TranslationSentenceUseCaseTests
{
    private readonly TrainingRepositoryTestHelper _training = new();
    private readonly ITranslationSentenceExerciseService _exerciseService = Substitute.For<ITranslationSentenceExerciseService>();

    [Fact]
    public async Task GetQuestionAsync_WhenNoAnswerInRequest_ReturnsQuestionFromNewNote()
    {
        var userId = Guid.NewGuid();
        var request = new TranslationSentenceRequest { DeckId = 10 };
        var targetNote = Note(id: 1, deckId: request.DeckId, userId: userId, back: "pies");
        var randomNotes = new List<Note>
        {
            Note(id: 2, deckId: request.DeckId, userId: userId, back: "kot"),
            Note(id: 3, deckId: request.DeckId, userId: userId, back: "dom")
        };

        _training.ReturnsNewNote(request.DeckId, userId, targetNote);
        _training.ReturnsRandomNotes(request.DeckId, targetNote.Id, 4, userId, randomNotes);
        _training.ReturnsRemainingCount(request.DeckId, userId, 12);

        _exerciseService.CreateQuestion(
                Arg.Is<string[]>(words => words.SequenceEqual(new[] { "kot", "dom" })),
                targetNote.Back,
                request.DeckId,
                userId,
                Arg.Any<CancellationToken>())
            .Returns("Translate this sentence");

        var useCase = CreateUseCase();

        var result = await useCase.GetQuestionAsync(request, userId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.NoteId.Should().Be(targetNote.Id);
        result.QuestionSentence.Should().Be("Translate this sentence");
        result.RemainingCount.Should().Be(12);

        await _exerciseService.DidNotReceiveWithAnyArgs()
            .CheckAnswer(default!, default);

        await _training.Repository.DidNotReceiveWithAnyArgs()
            .UpdateNote(default, default, default, default, default);
    }

    [Fact]
    public async Task GetQuestionAsync_WhenAnswerIsInRequest_ChecksAnswerAndUpdatesAnsweredNote()
    {
        var userId = Guid.NewGuid();
        var request = new TranslationSentenceRequest
        {
            DeckId = 10,
            NoteId = 99,
            AnswerSentence = "To jest pies",
            QuestionSentence = "This is a dog"
        };
        var answerResult = new TranslationSentenceResponse
        {
            IsCorrect = true,
            Verdict = "Correct",
            MoreCorrectSentence = "To jest pies"
        };
        var nextNote = Note(id: 1, deckId: request.DeckId, userId: userId, back: "pies");
        var randomNotes = new List<Note> { Note(id: 2, deckId: request.DeckId, userId: userId, back: "kot") };

        _exerciseService.CheckAnswer(
                Arg.Is<TranslationSentenceRequest>(actualRequest =>
                    actualRequest == request &&
                    actualRequest.UserId == userId),
                Arg.Any<CancellationToken>())
            .Returns(answerResult);

        _training.ReturnsNewNote(request.DeckId, userId, nextNote);
        _training.ReturnsRandomNotes(request.DeckId, nextNote.Id, 4, userId, randomNotes);
        _training.ReturnsRemainingCount(request.DeckId, userId, 6);

        _exerciseService.CreateQuestion(
                Arg.Is<string[]>(words => words.SequenceEqual(new[] { "kot" })),
                nextNote.Back,
                request.DeckId,
                userId,
                Arg.Any<CancellationToken>())
            .Returns("Next question");

        var useCase = CreateUseCase();

        var result = await useCase.GetQuestionAsync(request, userId, CancellationToken.None);

        result.Should().BeSameAs(answerResult);
        result!.IsCorrect.Should().BeTrue();
        result.NoteId.Should().Be(nextNote.Id);
        result.QuestionSentence.Should().Be("Next question");
        result.RemainingCount.Should().Be(6);

        await _training.Repository.Received(1)
            .UpdateNote(request.DeckId, request.NoteId, answerResult.IsCorrect, userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetQuestionAsync_WhenNoNewNoteExists_ReturnsQuestionFromReviewOrWeakNote()
    {
        var userId = Guid.NewGuid();
        var request = new TranslationSentenceRequest { DeckId = 10 };
        var reviewNote = Note(id: 7, deckId: request.DeckId, userId: userId, back: "pies");
        var randomNotes = new List<Note> { Note(id: 8, deckId: request.DeckId, userId: userId, back: "kot") };

        _training.ReturnsNewNote(request.DeckId, userId, null);
        _training.ReturnsReviewOrWeakNote(request.DeckId, userId, reviewNote);
        _training.ReturnsRandomNotes(request.DeckId, reviewNote.Id, 4, userId, randomNotes);
        _training.ReturnsRemainingCount(request.DeckId, userId, 3);

        _exerciseService.CreateQuestion(
                Arg.Is<string[]>(words => words.SequenceEqual(new[] { "kot" })),
                reviewNote.Back,
                request.DeckId,
                userId,
                Arg.Any<CancellationToken>())
            .Returns("Review question");

        var useCase = CreateUseCase();

        var result = await useCase.GetQuestionAsync(request, userId, CancellationToken.None);

        result.Should().NotBeNull();
        result!.NoteId.Should().Be(reviewNote.Id);
        result.QuestionSentence.Should().Be("Review question");

        await _training.Repository.Received(1)
            .GetReviewAndWeakNoteAsync(request.DeckId, userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetQuestionAsync_WhenNoAnswerAndNoTrainingNotesExist_ReturnsNull()
    {
        var userId = Guid.NewGuid();
        var request = new TranslationSentenceRequest { DeckId = 10 };

        _training.ReturnsNewNote(request.DeckId, userId, null);
        _training.ReturnsReviewOrWeakNote(request.DeckId, userId, null);

        var useCase = CreateUseCase();

        var result = await useCase.GetQuestionAsync(request, userId, CancellationToken.None);

        result.Should().BeNull();

        await _exerciseService.DidNotReceiveWithAnyArgs()
            .CreateQuestion(default!, default!, default, default, default);

        await _training.Repository.DidNotReceiveWithAnyArgs()
            .GetRandomNotesAsync(default, default, default, default, default);
    }

    [Fact]
    public async Task GetQuestionAsync_WhenAnswerWasCheckedButNoNextTrainingNotesExist_ReturnsVerdictWithZeroRemaining()
    {
        var userId = Guid.NewGuid();
        var request = new TranslationSentenceRequest
        {
            DeckId = 10,
            NoteId = 99,
            AnswerSentence = "bad answer"
        };
        var answerResult = new TranslationSentenceResponse
        {
            IsCorrect = false,
            Verdict = "Not quite",
            MoreCorrectSentence = "Better answer"
        };

        _exerciseService.CheckAnswer(request, Arg.Any<CancellationToken>())
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

        await _exerciseService.DidNotReceiveWithAnyArgs()
            .CreateQuestion(default!, default!, default, default, default);
    }

    private TranslationSentenceUseCase CreateUseCase()
    {
        return new TranslationSentenceUseCase(_training.Repository, _exerciseService);
    }
}
