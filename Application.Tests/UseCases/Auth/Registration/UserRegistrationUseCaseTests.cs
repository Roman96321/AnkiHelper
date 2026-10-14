using Application.Abstractions.Auth;
using Application.Exceptions;
using Application.Tests.TestData;
using Application.UseCases.Auth.Registration;
using Domain.Users;
using FluentAssertions;
using NSubstitute;

namespace Application.Tests.UseCases.Auth.Registration;

public class UserRegistrationUseCaseTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();

    [Fact]
    public async Task Register_WhenRequestIsValid_NormalizesEmailHashesPasswordAndAddsUser()
    {
        var request = new RegisterUserRequest
        {
            Email = "  USER@Example.COM  ",
            Password = "password123"
        };
        var createdFrom = DateTime.UtcNow;

        _userRepository.GetUserByEmail("user@example.com", Arg.Any<CancellationToken>())
            .Returns((User?)null);
        _passwordHasher.Generate(request.Password).Returns("hashed-password");

        var useCase = CreateUseCase();

        await useCase.Register(request, CancellationToken.None);

        await _userRepository.Received(1)
            .GetUserByEmail("user@example.com", Arg.Any<CancellationToken>());

        _passwordHasher.Received(1).Generate(request.Password);

        await _userRepository.Received(1)
            .AddUser(
                Arg.Is<User>(user =>
                    user.Email == "user@example.com" &&
                    user.PasswordHash == "hashed-password" &&
                    user.Id != Guid.Empty &&
                    user.CreatedAtUtc >= createdFrom &&
                    user.RefreshTokens.Count == 0),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_WhenEmailIsInvalid_ThrowsArgumentExceptionAndDoesNotAddUser()
    {
        var request = new RegisterUserRequest
        {
            Email = "not-email",
            Password = "password123"
        };

        var useCase = CreateUseCase();

        var act = () => useCase.Register(request, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Email is invalid.");

        await _userRepository.DidNotReceiveWithAnyArgs()
            .AddUser(default!, default);
        _passwordHasher.DidNotReceiveWithAnyArgs().Generate(default!);
    }

    [Fact]
    public async Task Register_WhenPasswordIsTooShort_ThrowsArgumentExceptionAndDoesNotAddUser()
    {
        var request = new RegisterUserRequest
        {
            Email = "user@example.com",
            Password = "short"
        };

        var useCase = CreateUseCase();

        var act = () => useCase.Register(request, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Password must be at least 8 characters long");

        await _userRepository.DidNotReceiveWithAnyArgs()
            .AddUser(default!, default);
        _passwordHasher.DidNotReceiveWithAnyArgs().Generate(default!);
    }

    [Fact]
    public async Task Register_WhenEmailAlreadyExists_ThrowsConflictExceptionAndDoesNotHashPassword()
    {
        var request = new RegisterUserRequest
        {
            Email = " USER@example.com ",
            Password = "password123"
        };
        var existingUser = AuthTestData.User(email: "user@example.com");

        _userRepository.GetUserByEmail("user@example.com", Arg.Any<CancellationToken>())
            .Returns(existingUser);

        var useCase = CreateUseCase();

        var act = () => useCase.Register(request, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("User with this email already exists");

        _passwordHasher.DidNotReceiveWithAnyArgs().Generate(default!);
        await _userRepository.DidNotReceiveWithAnyArgs()
            .AddUser(default!, default);
    }

    private UserRegistrationUseCase CreateUseCase()
    {
        return new UserRegistrationUseCase(_userRepository, _passwordHasher);
    }
}
