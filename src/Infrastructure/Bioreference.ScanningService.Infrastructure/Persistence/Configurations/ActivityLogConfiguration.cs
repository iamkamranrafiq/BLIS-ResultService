using Bioreference.ScanningService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioreference.ScanningService.Infrastructure.Persistence.Configurations;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("ActivityLog");

        builder.HasKey(x => x.ActivityLogId);
        builder.Property(x => x.ActivityLogId).HasColumnName("ActivityLogId");

        builder.Property(x => x.BatchId).HasColumnName("BatchId");

        builder.Property(x => x.UserId)
            .HasColumnName("UserId")
            .HasColumnType("varchar(255)")
            .IsRequired();

        builder.Property(x => x.DocumentId).HasColumnName("DocumentId");

        builder.Property(x => x.DocumentsScanned).HasColumnName("DocumentsScanned");
        builder.Property(x => x.PagesScanned).HasColumnName("PagesScanned");

        builder.Property(x => x.LogLevel)
            .HasColumnName("LogLevel")
            .HasColumnType("varchar(50)");

        builder.Property(x => x.QueueId).HasColumnName("QueueId");

        builder.Property(x => x.Description)
            .HasColumnName("Description")
            .HasColumnType("varchar(max)");

        builder.Property(x => x.LoggedDate)
            .HasColumnName("LoggedDate")
            .HasColumnType("datetime");

        builder.Property(x => x.CreatedDate)
            .HasColumnName("CreatedDate")
            .HasColumnType("datetime");

        builder.HasOne(x => x.Batch)
            .WithMany(x => x.ActivityLogs)
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.LoggedDate);
    }
}
