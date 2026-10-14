using Domain.Users;

namespace Application.Tests.TestData;

internal static class AuthTestData
{
    public static User User(
        Guid? id = null,
        string email = "user@example.com",
        string passwordHash = "password-hash")
    {
        return new User
        {
            Id = id ?? Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            CreatedAtUtc = DateTime.UtcNow,
            RefreshTokens = []
        };
    }

    public static RefreshToken RefreshToken(
        Guid? id = null,
        Guid? userId = null,
        string tokenHash = "refresh-token-hash",
        DateTime? expiresAtUtc = null,
        DateTime? revokedAtUtc = null)
    {
        return new RefreshToken
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            TokenHash = tokenHash,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = expiresAtUtc ?? DateTime.UtcNow.AddDays(30),
            RevokedAtUtc = revokedAtUtc
        };
    }
}
