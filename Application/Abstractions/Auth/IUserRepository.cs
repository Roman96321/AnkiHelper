using System;
using Domain.Users;

namespace Application.Abstractions.Auth
{
    public interface IUserRepository
    {
        Task AddUser(User user, CancellationToken token);
        Task<User?> GetUserByEmail(string email, CancellationToken token);
        Task<User> GetUserById(Guid id, CancellationToken token);
        Task AddRefreshToken(RefreshToken refreshToken, CancellationToken token);
        Task<RefreshToken?> GetRefreshTokenByHash(string hash, CancellationToken token);
        Task CleanUpRevokedRefreshTokens(CancellationToken token);
        Task<bool> TryRevokeRefreshToken(Guid refreshTokenId, DateTime revokedAtUtc, CancellationToken token);
    }
}
