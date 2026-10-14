using Domain.Deck;
using Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Deck> Decks => Set<Deck>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<DeckImportSettings> DeckImportSettings => Set<DeckImportSettings>();
    public DbSet<DeckImportFieldMapping> DeckImportFieldMappings => Set<DeckImportFieldMapping>();
    public DbSet<DailyNewNoteProgress> DailyNewNoteProgress => Set<DailyNewNoteProgress>();
    public DbSet<DailyTrainingStatistics> DailyTrainingStatistics => Set<DailyTrainingStatistics>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
