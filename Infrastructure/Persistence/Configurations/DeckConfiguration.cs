using Domain.Deck;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal class DeckConfiguration : IEntityTypeConfiguration<Deck>
{
    public void Configure(EntityTypeBuilder<Deck> builder)
    {
        builder.ToTable("decks");

        builder.HasKey(deck => new { deck.UserId, deck.Id });

        builder.Property(deck => deck.UserId)
            .IsRequired();

        builder.Property(deck => deck.Id)
            .ValueGeneratedNever();

        builder.Property(deck => deck.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasMany(deck => deck.Notes)
            .WithOne()
            .HasForeignKey(note => new { note.UserId, note.DeckId })
            .HasPrincipalKey(deck => new { deck.UserId, deck.Id })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(deck => deck.UserId);

        builder.Property(deck => deck.DeckDailyNewNotesCount)
            .IsRequired();

        builder.Property(deck => deck.UseOnlyLearnedNotes)
            .IsRequired();

        builder.Property(deck => deck.BackLanguage)
            .IsRequired();

        builder.Property(deck => deck.FrontLanguage)
            .IsRequired();
    }
}
