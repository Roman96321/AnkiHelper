using Application.Abstractions.Auth;
using Application.Tests.TestData;
using Application.UseCases.Auth.Login;
using Domain.Users;
using FluentAssertions;
using NSubstitute;
using DomainRefreshToken = Domain.Users.RefreshToken;

namespace Application.Tests.UseCases.Auth.Login;

public class UserLoginUseCaseTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IRefreshTokenProvider _refreshTokenProvider = Substitute.For<IRefreshTokenProvider>();
    private readonly IJwtProvider _jwtProvider = Substitute.For<IJwtProvider>();

    [Fact]
    public async Task Login_WhenCredentialsAreValid_ReturnsTokensAndStoresRefreshTokenHash()
    {
        var request = new LoginUserRequest
        {
            Email = " USER@Example.COM ",
            Password = "password123"
        };
        var user = AuthTestData.User(email: "user@example.com", passwordHash: "stored-hash");
        var createdFrom = DateTime.UtcNow;

        _userRepository.GetUserByEmail("user@example.com", Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher.Verify(request.Password, user.PasswordHash).Returns(true);
        _jwtProvider.GenerateToken(user).Returns("access-token");
        _refreshTokenProvider.Generate().Returns("raw-refresh-token");
        _refreshTokenProvider.Hash("raw-refresh-token").Returns("refresh-token-hash");

        var useCase = CreateUseCase();

        var response = await useCase.Login(request, CancellationToken.None);

        response.AccessToken.Should().Be("access-token");
        response.RefreshToken.Should().Be("raw-refresh-token");

        await _userRepository.Received(1)
            .GetUserByEmail("user@example.com", Arg.Any<CancellationToken>());
        _passwordHasher.Received(1).Verify(request.Password, user.PasswordHash);
        await _userRepository.Received(1).CleanUpRevokedRefreshTokens(Arg.Any<CancellationToken>());

        await _userRepository.Received(1)
            .AddRefreshToken(
                Arg.Is<DomainRefreshToken>(refreshToken =>
                    refreshToken.UserId == user.Id &&
                    refreshToken.TokenHash == "refresh-token-hash" &&
                    refreshToken.Id != Guid.Empty &&
                    refreshToken.CreatedAtUtc >= createdFrom &&
                    refreshToken.ExpiresAtUtc > createdFrom.AddDays(29) &&
                    refreshToken.RevokedAtUtc == null),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_WhenUserDoesNotExist_ThrowsUnauthorizedAndDoesNotVerifyPassword()
    {
        var request = new LoginUserRequest
        {
            Email = "missing@example.com",
            Password = "password123"
        };

        _userRepository.GetUserByEmail("missing@example.com", Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var useCase = CreateUseCase();

        var act = () => useCase.Login(request, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Failed to login");

        _passwordHasher.DidNotReceiveWithAnyArgs().Verify(default!, default!);
        await _userRepository.DidNotReceiveWithAnyArgs()
            .AddRefreshToken(default!, default);
    }

    [Fact]
    public async Task Login_WhenPasswordIsInvalid_ThrowsUnauthorizedAndDoesNotCreateTokens()
    {
        var request = new LoginUserRequest
        {
            Email = "user@example.com",
            Password = "wrong-password"
        };
        var user = AuthTestData.User(email: request.Email, passwordHash: "stored-hash");

        _userRepository.GetUserByEmail(request.Email, Arg.Any<CancellationToken>())
            .Returns(user);
        _passwordHasher.Verify(request.Password, user.PasswordHash).Returns(false);

        var useCase = CreateUseCase();

        var act = () => useCase.Login(request, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Failed to login");

        _jwtProvider.DidNotReceiveWithAnyArgs().GenerateToken(default!);
        _refreshTokenProvider.DidNotReceiveWithAnyArgs().Generate();
        await _userRepository.DidNotReceiveWithAnyArgs()
            .AddRefreshToken(default!, default);
    }

    private UserLoginUseCase CreateUseCase()
    {
        return new UserLoginUseCase(
            _userRepository,
            _passwordHasher,
            _refreshTokenProvider,
            _jwtProvider);
    }
}
