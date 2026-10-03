using Bioreference.ScanningService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioreference.ScanningService.Infrastructure.Persistence.Configurations;

public class BatchConfiguration : IEntityTypeConfiguration<Batch>
{
    public void Configure(EntityTypeBuilder<Batch> builder)
    {
        builder.ToTable("Batch");

        builder.HasKey(x => x.BatchId);
        builder.Property(x => x.BatchId)
            .HasColumnName("BatchId")
            .UseIdentityColumn()
            .ValueGeneratedOnAdd();

        builder.Property(x => x.BatchNumber)
            .HasColumnName("BatchNumber")
            .HasColumnType("varchar(255)")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("Name")
            .HasColumnType("varchar(255)")
            .IsRequired();

        builder.Property(x => x.ScanQueueId)
            .HasColumnName("ScanQueueId")
            .IsRequired(false);

        builder.Property(x => x.ScannerId).HasColumnName("ScannerId");

        builder.Property(x => x.ScanningFormatId)
            .HasColumnName("ScanningFormatId")
            .IsRequired(false);

        builder.Property(x => x.ScanMode)
            .HasColumnName("ScanMode")
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(x => x.BatchStatusId)
            .HasColumnName("BatchStatusID")
            .IsRequired(false);

        builder.Property(x => x.TotalDocuments).HasColumnName("TotalDocuments");
        builder.Property(x => x.TotalPages).HasColumnName("TotalPages");

        builder.Property(x => x.StartedDate)
            .HasColumnName("StartedDate")
            .HasColumnType("datetime")
            .IsRequired(false);

        builder.Property(x => x.CompletedDate)
            .HasColumnName("CompletedDate")
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

        builder.HasOne(x => x.ScanQueue)
            .WithMany(x => x.Batches)
            .HasForeignKey(x => x.ScanQueueId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ScanningFormat)
            .WithMany(x => x.Batches)
            .HasForeignKey(x => x.ScanningFormatId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.BatchStatus)
            .WithMany(x => x.Batches)
            .HasForeignKey(x => x.BatchStatusId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.BatchNumber).IsUnique();
    }
}
