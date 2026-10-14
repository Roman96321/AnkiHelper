using Domain.Deck;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.ToTable("notes");

        builder.HasKey(note => new { note.UserId, note.Id });

        builder.Property(note => note.UserId)
            .IsRequired();

        builder.Property(note => note.Id)
            .ValueGeneratedNever();

        builder.Property(note => note.DeckId)
            .IsRequired();

        builder.Property(note => note.Front)
            .IsRequired();

        builder.Property(note => note.Back)
            .IsRequired();

        builder.Property(note => note.NextReviewDate)
            .IsRequired();

        builder.HasIndex(note => new { note.UserId, note.DeckId });
    }
}
