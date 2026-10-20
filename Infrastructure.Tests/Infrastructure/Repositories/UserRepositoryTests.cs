using Infrastructure.Tests.Infrastructure.SQLServer;
using Application.Exceptions;
using Domain.Users;
using FluentAssertions;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using static Infrastructure.Tests.Infrastructure.SQLServer.InfrastructureTestData;

namespace Infrastructure.Tests.Infrastructure.Repositories;

[Collection(SQLServerCollection.Name)]
[Trait("Category", "Integration")]
public sealed class UserRepositoryTests(SQLServerTestFixture fixture) : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await fixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetUserByEmail_NormalizesEmailBeforeQuerying()
    {

        var user = User(email: "user@example.com");

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        }

        await using var queryContext = fixture.CreateDbContext();
        var repository = new UserRepository(queryContext);

        var foundUser = await repository.GetUserByEmail("  USER@EXAMPLE.COM  ", CancellationToken.None);

        foundUser.Should().NotBeNull();
        foundUser!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task RefreshTokenMethods_AddFindRevokeAndHideRevokedToken()
    {

        var user = User();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "refresh-token-hash",
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30)
        };

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new UserRepository(dbContext);

            await repository.AddRefreshToken(refreshToken, CancellationToken.None);
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new UserRepository(dbContext);

            var foundToken = await repository.GetRefreshTokenByHash("refresh-token-hash", CancellationToken.None);

            foundToken.Should().NotBeNull();
            foundToken!.Id.Should().Be(refreshToken.Id);

            var wasRevoked = await repository.TryRevokeRefreshToken(refreshToken.Id, DateTime.UtcNow, CancellationToken.None);

            wasRevoked.Should().BeTrue();
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new UserRepository(dbContext);

            var revokedToken = await repository.GetRefreshTokenByHash("refresh-token-hash", CancellationToken.None);

            revokedToken.Should().BeNull();
        }
    }

    [Fact]
    public async Task CleanUpRevokedRefreshTokens_DeletesOnlyExpiredRevokedTokens()
    {

        var user = User();
        var expiredRevokedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "expired-revoked",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-40),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1),
            RevokedAtUtc = DateTime.UtcNow.AddDays(-2)
        };
        var activeToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "active",
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30)
        };
        var revokedButNotExpiredToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "revoked-not-expired",
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RevokedAtUtc = DateTime.UtcNow
        };

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Users.Add(user);
            dbContext.RefreshTokens.AddRange(expiredRevokedToken, activeToken, revokedButNotExpiredToken);
            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new UserRepository(dbContext);

            await repository.CleanUpRevokedRefreshTokens(CancellationToken.None);
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var tokenHashes = await dbContext.RefreshTokens
                .Select(refreshToken => refreshToken.TokenHash)
                .ToListAsync();

            tokenHashes.Should().BeEquivalentTo("active", "revoked-not-expired");
        }
    }

    [Fact]
    public async Task AddUser_StoresUserAndGetUserByIdReturnsIt()
    {

        var user = User(email: "new-user@example.com");

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new UserRepository(dbContext);

            await repository.AddUser(user, CancellationToken.None);
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var repository = new UserRepository(dbContext);

            var savedUser = await repository.GetUserById(user.Id, CancellationToken.None);

            savedUser.Id.Should().Be(user.Id);
            savedUser.Email.Should().Be("new-user@example.com");
            savedUser.PasswordHash.Should().Be("password-hash");
        }
    }

    [Fact]
    public async Task AddUser_WhenEmailAlreadyExists_ThrowsConflictException()
    {
        var existingUser = User(email: "duplicate@example.com");
        var duplicateUser = User(email: "duplicate@example.com");

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Users.Add(existingUser);
            await dbContext.SaveChangesAsync();
        }

        await using var duplicateContext = fixture.CreateDbContext();
        var repository = new UserRepository(duplicateContext);

        var act = () => repository.AddUser(duplicateUser, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("User with this email already exists");
    }

    [Fact]
    public async Task GetUserById_WhenUserDoesNotExist_ThrowsUnauthorizedAccessException()
    {

        await using var dbContext = fixture.CreateDbContext();
        var repository = new UserRepository(dbContext);

        var act = () => repository.GetUserById(Guid.NewGuid(), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User not found");
    }

    [Fact]
    public async Task GetRefreshTokenByHash_WhenTokenDoesNotExist_ReturnsNull()
    {

        var user = User();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "existing-token",
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30)
        };

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Users.Add(user);
            dbContext.RefreshTokens.Add(refreshToken);
            await dbContext.SaveChangesAsync();
        }

        await using var queryContext = fixture.CreateDbContext();
        var repository = new UserRepository(queryContext);

        var missingToken = await repository.GetRefreshTokenByHash("missing-token", CancellationToken.None);

        missingToken.Should().BeNull();
    }

    [Fact]
    public async Task TryRevokeRefreshToken_WhenTokenIsAlreadyRevoked_ReturnsFalse()
    {
        var user = User();
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "already-revoked",
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            RevokedAtUtc = DateTime.UtcNow.AddMinutes(-1)
        };

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Users.Add(user);
            dbContext.RefreshTokens.Add(refreshToken);
            await dbContext.SaveChangesAsync();
        }

        await using var queryContext = fixture.CreateDbContext();
        var repository = new UserRepository(queryContext);

        var wasRevoked = await repository.TryRevokeRefreshToken(refreshToken.Id, DateTime.UtcNow, CancellationToken.None);

        wasRevoked.Should().BeFalse();
    }
}
