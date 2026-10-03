using Bioreference.ScanningService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioreference.ScanningService.Infrastructure.Persistence.Configurations;

public class ScanQueueConfiguration : IEntityTypeConfiguration<ScanQueue>
{
    public void Configure(EntityTypeBuilder<ScanQueue> builder)
    {
        builder.ToTable("ScanQueue");

        builder.HasKey(x => x.ScanQueueId);
        builder.Property(x => x.ScanQueueId).HasColumnName("ScanQueueId");

        builder.Property(x => x.Name)
            .HasColumnName("Name")
            .HasColumnType("varchar(255)")
            .IsRequired();

        builder.Property(x => x.IsAutoCommit).HasColumnName("IsAutoCommit");

        builder.Property(x => x.LastUsedDate)
            .HasColumnName("LastUsedDate")
            .HasColumnType("datetime")
            .IsRequired(false);

        builder.Property(x => x.IsActive).HasColumnName("IsActive");

        builder.Property(x => x.CreatedBy)
            .HasColumnName("CreatedBy")
            .HasColumnType("varchar(255)")
            .IsRequired();

        builder.Property(x => x.CreatedDate)
            .HasColumnName("CreatedDate")
            .HasColumnType("datetime");

        builder.Property(x => x.UpdatedBy)
            .HasColumnName("UpdatedBy")
            .HasColumnType("varchar(255)")
            .IsRequired();

        builder.Property(x => x.UpdatedDate)
            .HasColumnName("UpdatedDate")
            .HasColumnType("datetime");

        builder.Property(x => x.IsDeleted).HasColumnName("IsDeleted");
    }
}
