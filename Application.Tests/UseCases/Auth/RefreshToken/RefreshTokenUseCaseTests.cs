using Application.Abstractions.Auth;
using Application.Tests.TestData;
using Application.UseCases.Auth.RefreshTokens;
using Domain.Users;
using FluentAssertions;
using NSubstitute;
using DomainRefreshToken = Domain.Users.RefreshToken;

namespace Application.Tests.UseCases.Auth.RefreshToken;

public class RefreshTokenUseCaseTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenProvider _refreshTokenProvider = Substitute.For<IRefreshTokenProvider>();
    private readonly IJwtProvider _jwtProvider = Substitute.For<IJwtProvider>();

    [Fact]
    public async Task RefreshAsync_WhenRefreshTokenIsValid_RotatesRefreshTokenAndReturnsNewTokens()
    {
        var user = AuthTestData.User();
        var existingRefreshToken = AuthTestData.RefreshToken(
            userId: user.Id,
            tokenHash: "old-refresh-token-hash",
            expiresAtUtc: DateTime.UtcNow.AddDays(5));
        var createdFrom = DateTime.UtcNow;

        _refreshTokenProvider.Hash("old-raw-refresh-token").Returns("old-refresh-token-hash");
        _userRepository.GetRefreshTokenByHash("old-refresh-token-hash", Arg.Any<CancellationToken>())
            .Returns(existingRefreshToken);
        _userRepository.GetUserById(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);
        _jwtProvider.GenerateToken(user).Returns("new-access-token");
        _refreshTokenProvider.Generate().Returns("new-raw-refresh-token");
        _refreshTokenProvider.Hash("new-raw-refresh-token").Returns("new-refresh-token-hash");
        _userRepository.TryRevokeRefreshToken(existingRefreshToken.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var useCase = CreateUseCase();

        var response = await useCase.RefreshAsync(new RefreshTokenRequest("old-raw-refresh-token"), CancellationToken.None);

        response.AccessToken.Should().Be("new-access-token");
        response.RefreshToken.Should().Be("new-raw-refresh-token");

        await _userRepository.Received(1)
            .TryRevokeRefreshToken(existingRefreshToken.Id, Arg.Is<DateTime>(value => value >= createdFrom), Arg.Any<CancellationToken>());

        await _userRepository.Received(1)
            .AddRefreshToken(
                Arg.Is<DomainRefreshToken>(refreshToken =>
                    refreshToken.UserId == user.Id &&
                    refreshToken.TokenHash == "new-refresh-token-hash" &&
                    refreshToken.Id != Guid.Empty &&
                    refreshToken.CreatedAtUtc >= createdFrom &&
                    refreshToken.ExpiresAtUtc > createdFrom.AddDays(29) &&
                    refreshToken.RevokedAtUtc == null),
                Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task RefreshAsync_WhenRefreshTokenIsEmpty_ThrowsUnauthorized(string refreshToken)
    {
        var useCase = CreateUseCase();

        var act = () => useCase.RefreshAsync(new RefreshTokenRequest(refreshToken), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Refresh token is required.");

        _refreshTokenProvider.DidNotReceiveWithAnyArgs().Hash(default!);
        await _userRepository.DidNotReceiveWithAnyArgs()
            .GetRefreshTokenByHash(default!, default);
    }

    [Fact]
    public async Task RefreshAsync_WhenRefreshTokenDoesNotExist_ThrowsUnauthorized()
    {
        _refreshTokenProvider.Hash("raw-refresh-token").Returns("refresh-token-hash");
        _userRepository.GetRefreshTokenByHash("refresh-token-hash", Arg.Any<CancellationToken>())
            .Returns((DomainRefreshToken?)null);

        var useCase = CreateUseCase();

        var act = () => useCase.RefreshAsync(new RefreshTokenRequest("raw-refresh-token"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid refresh token.");

        await AssertNoRefreshTokenRotation();
    }

    [Fact]
    public async Task RefreshAsync_WhenRefreshTokenIsRevoked_ThrowsUnauthorized()
    {
        var refreshToken = AuthTestData.RefreshToken(revokedAtUtc: DateTime.UtcNow.AddMinutes(-1));

        _refreshTokenProvider.Hash("raw-refresh-token").Returns(refreshToken.TokenHash);
        _userRepository.GetRefreshTokenByHash(refreshToken.TokenHash, Arg.Any<CancellationToken>())
            .Returns(refreshToken);

        var useCase = CreateUseCase();

        var act = () => useCase.RefreshAsync(new RefreshTokenRequest("raw-refresh-token"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid refresh token.");

        await AssertNoRefreshTokenRotation();
    }

    [Fact]
    public async Task RefreshAsync_WhenRefreshTokenIsExpired_ThrowsUnauthorized()
    {
        var refreshToken = AuthTestData.RefreshToken(expiresAtUtc: DateTime.UtcNow.AddMinutes(-1));

        _refreshTokenProvider.Hash("raw-refresh-token").Returns(refreshToken.TokenHash);
        _userRepository.GetRefreshTokenByHash(refreshToken.TokenHash, Arg.Any<CancellationToken>())
            .Returns(refreshToken);

        var useCase = CreateUseCase();

        var act = () => useCase.RefreshAsync(new RefreshTokenRequest("raw-refresh-token"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid refresh token.");

        await AssertNoRefreshTokenRotation();
    }

    [Fact]
    public async Task RefreshAsync_WhenRefreshTokenWasAlreadyRevokedConcurrently_ThrowsUnauthorized()
    {
        var user = AuthTestData.User();
        var existingRefreshToken = AuthTestData.RefreshToken(
            userId: user.Id,
            tokenHash: "old-refresh-token-hash",
            expiresAtUtc: DateTime.UtcNow.AddDays(5));

        _refreshTokenProvider.Hash("old-raw-refresh-token").Returns("old-refresh-token-hash");
        _userRepository.GetRefreshTokenByHash("old-refresh-token-hash", Arg.Any<CancellationToken>())
            .Returns(existingRefreshToken);
        _userRepository.GetUserById(user.Id, Arg.Any<CancellationToken>())
            .Returns(user);
        _jwtProvider.GenerateToken(user).Returns("new-access-token");
        _refreshTokenProvider.Generate().Returns("new-raw-refresh-token");
        _refreshTokenProvider.Hash("new-raw-refresh-token").Returns("new-refresh-token-hash");
        _userRepository.TryRevokeRefreshToken(existingRefreshToken.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var useCase = CreateUseCase();

        var act = () => useCase.RefreshAsync(new RefreshTokenRequest("old-raw-refresh-token"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid refresh token.");

        await _userRepository.DidNotReceiveWithAnyArgs()
            .AddRefreshToken(default!, default);
    }

    private async Task AssertNoRefreshTokenRotation()
    {
        _jwtProvider.DidNotReceiveWithAnyArgs().GenerateToken(default!);
        _refreshTokenProvider.DidNotReceiveWithAnyArgs().Generate();
        await _userRepository.DidNotReceiveWithAnyArgs()
            .TryRevokeRefreshToken(default, default, default);
        await _userRepository.DidNotReceiveWithAnyArgs()
            .AddRefreshToken(default!, default);
    }

    private RefreshTokenUseCase CreateUseCase()
    {
        return new RefreshTokenUseCase(
            _userRepository,
            _refreshTokenProvider,
            _jwtProvider);
    }
}
