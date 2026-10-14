using Application.Abstractions.Auth;
using Domain.Users;

namespace Application.UseCases.Auth.RefreshTokens
{
    public class RefreshTokenUseCase(
        IUserRepository userRepository,
        IRefreshTokenProvider refreshTokenProvider,
        IJwtProvider jwtProvider)
    {
        public async Task<AuthTokensResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                throw new UnauthorizedAccessException("Refresh token is required.");

            var refreshTokenHash = refreshTokenProvider.Hash(request.RefreshToken);
            var existingRefreshToken = await userRepository.GetRefreshTokenByHash(refreshTokenHash, token);

            if (existingRefreshToken is null ||
                existingRefreshToken.RevokedAtUtc is not null ||
                existingRefreshToken.ExpiresAtUtc <= DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Invalid refresh token.");
            }

            var user = await userRepository.GetUserById(existingRefreshToken.UserId, token);

            var newAccessToken = jwtProvider.GenerateToken(user);
            var newRefreshToken = refreshTokenProvider.Generate();

            var newRefreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = refreshTokenProvider.Hash(newRefreshToken),
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(30)
            };

            var tokenWasRevoked = await userRepository.TryRevokeRefreshToken(existingRefreshToken.Id, DateTime.UtcNow, token);

            if (!tokenWasRevoked)
                throw new UnauthorizedAccessException("Invalid refresh token.");

            await userRepository.AddRefreshToken(newRefreshTokenEntity, token);

            return new AuthTokensResponse(newAccessToken, newRefreshToken);
        }
    }
}
