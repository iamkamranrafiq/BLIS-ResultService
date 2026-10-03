using Bioreference.ScanningService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Security.Cryptography.X509Certificates;

namespace Bioreference.ScanningService.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Document");

        builder.HasKey(x => x.DocumentId);
        builder.Property(x => x.DocumentId).HasColumnName("DocumentId");

        builder.Property(x => x.BatchId)
            .HasColumnName("BatchId")
            .IsRequired(false);

        builder.Property(x => x.DocumentTypeId).HasColumnName("DocumentTypeId");

        builder.Property(x => x.Name)
            .HasColumnName("Name")
            .HasColumnType("varchar(500)")
            .IsRequired();
        builder.Property(x => x.ShortName)
            .HasColumnName("ShortName")
            .HasColumnType("varchar(100)")
            .IsRequired(false);
        builder.Property(x => x.BarcodeValue)
            .HasColumnName("BarcodeValue")
            .HasColumnType("varchar(255)")
            .IsRequired(false);

        builder.Property(x => x.IndexingValue)
            .HasColumnName("IndexingValue")
            .HasColumnType("varchar(500)")
            .IsRequired(false);

        builder.Property(x => x.DocUrl)
            .HasColumnName("DocUrl")
            .HasColumnType("varchar(2000)")
            .IsRequired(false);

        builder.Property(x => x.DocumentDate)
            .HasColumnName("DocumentDate")
            .HasColumnType("date")
            .IsRequired(false);

        builder.Property(x => x.SpecimenNumber)
            .HasColumnName("SpecimenNumber")
            .HasColumnType("varchar(255)")
            .IsRequired(false);

        builder.Property(x => x.DatePosted)
            .HasColumnName("DatePosted")
            .HasColumnType("datetime");

        builder.Property(x => x.Status)
            .HasColumnName("Status")
            .HasColumnType("varchar(50)")
            .IsRequired();
        builder.Property(e => e.ThumbnailUrl)
           .HasMaxLength(500)
           .IsUnicode(false);

        builder.Property(x => x.IsIndexed).HasColumnName("IsIndexed");
        builder.Property(x => x.TotalPages).HasColumnName("TotalPages");
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

        builder.HasOne(x => x.Batch)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.DocumentType)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.DocumentTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.BarcodeValue);
        builder.HasIndex(x => x.BatchId);
    }
}
