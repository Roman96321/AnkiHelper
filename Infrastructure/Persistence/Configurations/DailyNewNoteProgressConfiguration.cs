using Domain.Deck;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Infrastructure.Persistence.Configurations
{
    internal class DailyNewNoteProgressConfiguration : IEntityTypeConfiguration<DailyNewNoteProgress>
    {
        public void Configure(EntityTypeBuilder<DailyNewNoteProgress> builder)
        {
            builder.ToTable("daily_new_note_progress");

            builder.HasKey(x => new { x.UserId, x.DeckId, x.Date });

            builder.Property(x => x.UserId).IsRequired();

            builder.Property(x => x.DeckId).IsRequired();

            builder.Property(x => x.Date).IsRequired();
            builder.Property(x => x.NewAnsweredCount);

            builder.HasOne<Deck>()
            .WithMany()
            .HasForeignKey(x => new { x.UserId, x.DeckId })
            .HasPrincipalKey(x => new { x.UserId, x.Id })
            .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
