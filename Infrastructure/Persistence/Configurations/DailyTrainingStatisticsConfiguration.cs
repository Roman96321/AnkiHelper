using Domain.Deck;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Infrastructure.Persistence.Configurations
{
    internal class DailyTrainingStatisticsConfiguration : IEntityTypeConfiguration<DailyTrainingStatistics>
    {
        public void Configure(EntityTypeBuilder<DailyTrainingStatistics> builder)
        {
            builder.ToTable("daily_training_statistics");

            builder.HasKey(x => new { x.UserId, x.Date });

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.Date).
                IsRequired();

            builder.Property(x => x.LearningNoteCounts)
                .IsRequired();
        }
    }
}
