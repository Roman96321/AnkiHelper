using Application.Abstractions.OpenAI;
using Application.Tests.TestData;
using Application.UseCases.Training.WordOrder;
using Domain.Deck;
using FluentAssertions;
using NSubstitute;
using static Application.Tests.TestData.TrainingRepositoryTestHelper;

namespace Application.Tests.UseCases.Training.WordOrder;

public class WordOrderUseCaseTests
{
    private readonly TrainingRepositoryTestHelper _training = new();
    private readonly IWordOrderQuestionBuilder _wordOrderQuestionBuilder = Substitute.For<IWordOrderQuestionBuilder>();

    [Fact]
    public async Task GetQuestionAsync_WhenNoAnswerInRequest_ReturnsQuestionFromNewNote()
    {
        var userId = Guid.NewGuid();
        var request = new WordOrderRequest { DeckId = 10 };
        var trainingNote = Note(id: 1, deckId: request.DeckId, userId: userId, front: "Hello", back: "Hola");
        var randomNotes = new List<Note>
        {
            Note(id: 2, deckId: request.DeckId, userId: userId, front: "love", back: "encantan"),
            Note(id: 3, deckId: request.DeckId, userId: userId, front: "cars", back: "coches")
        };
        var question = Question(trainingNote.Id);

        _training.ReturnsNewNote(request.DeckId, userId, trainingNote);
        _training.ReturnsRandomNotes(request.DeckId, trainingNote.Id, userId, randomNotes);
        _training.ReturnsRemainingCount(request.DeckId, userId, 20);

        _wordOrderQuestionBuilder.BuildQuestion(trainingNote, randomNotes, Arg.Any<CancellationToken>())
            .Returns(question);

        var useCase = CreateUseCase();

        var result = await useCase.GetQuestionAsync(request, userId, CancellationToken.None);

        result.Should().BeSameAs(question);
        result!.RemainingCount.Should().Be(20);

        await _training.Repository.DidNotReceiveWithAnyArgs()
            .UpdateNote(default, default, default, default, default);

        await _training.Repository.DidNotReceiveWithAnyArgs()
            .GetReviewAndWeakNoteAsync(default, default, default);
    }

    [Fact]
    public async Task GetQuestionAsync_WhenAnswerIsInRequest_UpdatesAnsweredNoteBeforeReturningNextQuestion()
    {
        var userId = Guid.NewGuid();
        var request = new WordOrderRequest
        {
            DeckId = 10,
            NoteId = 99,
            IsCorrectAnswer = true
        };
        var nextNote = Note(id: 1, deckId: request.DeckId, userId: userId);
        var randomNotes = new List<Note> { Note(id: 2, deckId: request.DeckId, userId: userId) };
        var question = Question(nextNote.Id);

        _training.ReturnsNewNote(request.DeckId, userId, nextNote);
        _training.ReturnsRandomNotes(request.DeckId, nextNote.Id, userId, randomNotes);
        _training.ReturnsRemainingCount(request.DeckId, userId, 5);

        _wordOrderQuestionBuilder.BuildQuestion(nextNote, randomNotes, Arg.Any<CancellationToken>())
            .Returns(question);

        var useCase = CreateUseCase();

        var result = await useCase.GetQuestionAsync(request, userId, CancellationToken.None);

        result.Should().BeSameAs(question);
        result!.RemainingCount.Should().Be(5);

        await _training.Repository.Received(1)
            .UpdateNote(request.DeckId, request.NoteId, request.IsCorrectAnswer, userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetQuestionAsync_WhenNoNewNoteExists_ReturnsQuestionFromReviewOrWeakNote()
    {
        var userId = Guid.NewGuid();
        var request = new WordOrderRequest { DeckId = 10 };
        var reviewNote = Note(id: 7, deckId: request.DeckId, userId: userId);
        var randomNotes = new List<Note> { Note(id: 8, deckId: request.DeckId, userId: userId) };
        var question = Question(reviewNote.Id);

        _training.ReturnsNewNote(request.DeckId, userId, null);
        _training.ReturnsReviewOrWeakNote(request.DeckId, userId, reviewNote);
        _training.ReturnsRandomNotes(request.DeckId, reviewNote.Id, userId, randomNotes);
        _training.ReturnsRemainingCount(request.DeckId, userId, 3);

        _wordOrderQuestionBuilder.BuildQuestion(reviewNote, randomNotes, Arg.Any<CancellationToken>())
            .Returns(question);

        var useCase = CreateUseCase();

        var result = await useCase.GetQuestionAsync(request, userId, CancellationToken.None);

        result.Should().BeSameAs(question);
        result!.NoteId.Should().Be(reviewNote.Id);

        await _training.Repository.Received(1)
            .GetReviewAndWeakNoteAsync(request.DeckId, userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetQuestionAsync_WhenNoNewReviewOrWeakNoteExists_ReturnsNull()
    {
        var userId = Guid.NewGuid();
        var request = new WordOrderRequest { DeckId = 10 };

        _training.ReturnsNewNote(request.DeckId, userId, null);
        _training.ReturnsReviewOrWeakNote(request.DeckId, userId, null);

        var useCase = CreateUseCase();

        var result = await useCase.GetQuestionAsync(request, userId, CancellationToken.None);

        result.Should().BeNull();

        await _training.Repository.DidNotReceiveWithAnyArgs()
            .GetRandomNotesAsync(default, default, default, default, default);

        await _wordOrderQuestionBuilder.DidNotReceiveWithAnyArgs()
            .BuildQuestion(default!, default!, default);

        await _training.Repository.DidNotReceiveWithAnyArgs()
            .GetRemainingNotesCountAsync(default, default, default);
    }

    [Fact]
    public async Task GetQuestionAsync_PassesTrainingNoteAndRandomNotesToQuestionBuilder()
    {
        var userId = Guid.NewGuid();
        var request = new WordOrderRequest { DeckId = 10 };
        var trainingNote = Note(id: 1, deckId: request.DeckId, userId: userId);
        var randomNotes = new List<Note>
        {
            Note(id: 2, deckId: request.DeckId, userId: userId),
            Note(id: 3, deckId: request.DeckId, userId: userId)
        };
        var question = Question(trainingNote.Id);

        _training.ReturnsNewNote(request.DeckId, userId, trainingNote);
        _training.ReturnsRandomNotes(request.DeckId, trainingNote.Id, userId, randomNotes);

        _wordOrderQuestionBuilder.BuildQuestion(trainingNote, randomNotes, Arg.Any<CancellationToken>())
            .Returns(question);

        var useCase = CreateUseCase();

        await useCase.GetQuestionAsync(request, userId, CancellationToken.None);

        await _wordOrderQuestionBuilder.Received(1)
            .BuildQuestion(trainingNote, randomNotes, Arg.Any<CancellationToken>());
    }

    private WordOrderUseCase CreateUseCase()
    {
        return new WordOrderUseCase(_wordOrderQuestionBuilder, _training.Repository);
    }

    private static WordOrderQuestion Question(long noteId)
    {
        return new WordOrderQuestion
        {
            NoteId = noteId,
            CorrectSentence = "I love cars and milk",
            Translation = "Me encantan los coches y la leche",
            Words = ["I", "love", "cars", "and", "milk"]
        };
    }
}
