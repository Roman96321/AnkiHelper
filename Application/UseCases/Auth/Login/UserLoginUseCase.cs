using Application.Abstractions.Auth;
using Domain.Users;
using System;

namespace Application.UseCases.Auth.Login
{
    public class UserLoginUseCase(IUserRepository userRepository, IPasswordHasher passwordHasher, IRefreshTokenProvider refreshTokenProvider, IJwtProvider jwtProvider)
    {
        public async Task<AuthTokensResponse> Login(LoginUserRequest request, CancellationToken token)
        {
            var user = await userRepository.GetUserByEmail(request.Email.Trim().ToLower(), token);

            if (user is null)
                throw new UnauthorizedAccessException("Failed to login");

            var result = passwordHasher.Verify(request.Password, user.PasswordHash);

            if (!result)
                throw new UnauthorizedAccessException("Failed to login");

            var accessToken = jwtProvider.GenerateToken(user);
            var refreshToken = refreshTokenProvider.Generate();

            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = refreshTokenProvider.Hash(refreshToken),
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(30)
            };

            await userRepository.CleanUpRevokedRefreshTokens(token);
            await userRepository.AddRefreshToken(refreshTokenEntity, token);

            return new AuthTokensResponse(accessToken, refreshToken);
        }
    }
}
