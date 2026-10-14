using Domain.Deck;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Infrastructure.Persistence.Configurations
{
    internal class DeckImportSettingsConfiguration : IEntityTypeConfiguration<DeckImportSettings>
    {
        public void Configure(EntityTypeBuilder<DeckImportSettings> builder)
        {
            builder.ToTable("deck_import_settings");

            builder.HasKey(deck => new { deck.UserId, deck.DeckId });

            builder.Property(deck => deck.UserId)
                .IsRequired();

            builder.Property(deck => deck.DeckId)
                .ValueGeneratedNever();

            builder.Property(deck => deck.DeckName)
                .IsRequired()
                .HasMaxLength(248);

            builder.HasMany(deck => deck.FieldMappings)
                .WithOne()
                .HasForeignKey(note => new { note.UserId, note.DeckId })
                .HasPrincipalKey(deck => new { deck.UserId, deck.DeckId })
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(deck => deck.UserId);
        }
    }

    internal class DeckImportFieldMappingConfiguration : IEntityTypeConfiguration<DeckImportFieldMapping>
    {
        public void Configure(EntityTypeBuilder<DeckImportFieldMapping> builder)
        {
            builder.ToTable("deck_import_field_mappings");

            builder.HasKey(note => note.Id);

            builder.Property(note => note.UserId)
                .IsRequired();

            builder.Property(note => note.DeckId)
                .IsRequired();

            builder.Property(note => note.FrontKey)
                .IsRequired()
                .HasMaxLength(248);

            builder.Property(note => note.BackKey)
                .IsRequired()
                .HasMaxLength(248);

            builder.HasIndex(note => new { note.UserId, note.DeckId });
        }
    }
}
