using Bioreference.ScanningService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioreference.ScanningService.Infrastructure.Persistence.Configurations;

public class ScanQueueDocumentTypeConfiguration : IEntityTypeConfiguration<ScanQueueDocumentType>
{
    public void Configure(EntityTypeBuilder<ScanQueueDocumentType> builder)
    {
        builder.ToTable("ScanQueueDocumentType");

        builder.HasKey(x => x.ScanQueueDocumentTypeId);
        builder.Property(x => x.ScanQueueDocumentTypeId).HasColumnName("ScanQueueDocumentTypeId");

        builder.Property(x => x.ScanQueueId).HasColumnName("ScanQueueId");
        builder.Property(x => x.DocumentTypeId).HasColumnName("DocumentTypeId");

        builder.Property(x => x.CreatedBy)
            .HasColumnName("CreatedBy")
            .HasColumnType("varchar(255)")
            .IsRequired();

        builder.Property(x => x.CreatedDate)
            .HasColumnName("CreatedDate")
            .HasColumnType("datetime");

        builder.HasOne(x => x.ScanQueue)
            .WithMany(x => x.ScanQueueDocumentTypes)
            .HasForeignKey(x => x.ScanQueueId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.DocumentType)
            .WithMany(x => x.ScanQueueDocumentTypes)
            .HasForeignKey(x => x.DocumentTypeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
