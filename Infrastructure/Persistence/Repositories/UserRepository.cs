using Microsoft.EntityFrameworkCore;
using Application.Abstractions.Auth;
using Application.Exceptions;
using Domain.Users;
using Microsoft.Data.SqlClient;
using System;

namespace Infrastructure.Persistence.Repositories
{
    internal class UserRepository(AppDbContext dbContext) : IUserRepository
    {
        public async Task AddUser(User user, CancellationToken token)
        {
            try
            {
                await dbContext.Users.AddAsync(user, token);
                await dbContext.SaveChangesAsync(token);
            }
            catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2627 or 2601 })
            {
                throw new ConflictException("User with this email already exists");
            }
        }

        public async Task<User?> GetUserByEmail(string email, CancellationToken token)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            var user = await dbContext.
                Users.
                AsNoTracking().
                FirstOrDefaultAsync(u => u.Email == normalizedEmail, token);

            return user;
        }

        public async Task AddRefreshToken(RefreshToken refreshToken, CancellationToken token)
        {
            await dbContext.RefreshTokens.AddAsync(refreshToken, token);
            await dbContext.SaveChangesAsync(token);
        }

        public async Task<RefreshToken?> GetRefreshTokenByHash(string hash, CancellationToken token)
        {
            var refreshToken = await dbContext.
                RefreshTokens.
                AsNoTracking().
                FirstOrDefaultAsync(rt => rt.TokenHash == hash && !rt.RevokedAtUtc.HasValue, token);

            return refreshToken;
        }

        public async Task<bool> TryRevokeRefreshToken(Guid refreshTokenId, DateTime revokedAtUtc, CancellationToken token)
        {
            var updatedRows = await dbContext.
                RefreshTokens.
                Where(refreshToken => refreshToken.Id == refreshTokenId && refreshToken.RevokedAtUtc == null).
                ExecuteUpdateAsync(setters => setters
                    .SetProperty(refreshToken => refreshToken.RevokedAtUtc, revokedAtUtc), token);

            return updatedRows == 1;
        }

        public async Task CleanUpRevokedRefreshTokens(CancellationToken token)
        {
            var now = DateTime.UtcNow;

            await dbContext.
                RefreshTokens.
                Where(rt => rt.RevokedAtUtc.HasValue && rt.ExpiresAtUtc <= now).
                ExecuteDeleteAsync(token);
        }

        public async Task<User> GetUserById(Guid id, CancellationToken token)
        {
            var user = await dbContext.
                Users.
                AsNoTracking().
                FirstOrDefaultAsync(u => u.Id == id, token);

            return user ?? throw new UnauthorizedAccessException("User not found");
        }
    }
}
