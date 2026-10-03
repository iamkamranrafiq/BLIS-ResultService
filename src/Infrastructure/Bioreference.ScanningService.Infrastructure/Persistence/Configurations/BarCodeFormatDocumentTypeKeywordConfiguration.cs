using Bioreference.ScanningService.Domain.Common;
using Bioreference.ScanningService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioreference.ScanningService.Infrastructure.Persistence.Configurations;

public class BarCodeFormatDocumentTypeKeywordConfiguration : IEntityTypeConfiguration<BarCodeFormatDocumentTypeKeyword>
{
    public void Configure(EntityTypeBuilder<BarCodeFormatDocumentTypeKeyword> builder)
    {
        builder.ToTable("BarCodeFormatDocumentTypeKeyword");

        builder.HasKey(e => e.BarCodeDocumentTypeId).HasName("PK_BarCodeDocumentType");

        builder.ToTable("BarCodeFormatDocumentTypeKeyword");

        builder.Property(e => e.CreatedBy)
            .IsRequired()
            .HasMaxLength(255)
            .IsUnicode(false);
        builder.Property(e => e.CreatedDate).HasColumnType("datetime");
        builder.Property(e => e.HeightB).HasColumnType("decimal(18, 0)");
        builder.Property(e => e.LeftB).HasColumnType("decimal(18, 0)");
        builder.Property(e => e.TopB).HasColumnType("decimal(18, 0)");
        builder.Property(e => e.UpdatedBy)
            .HasMaxLength(255)
            .IsUnicode(false);
        builder.Property(e => e.UpdatedDate).HasColumnType("datetime");
        builder.Property(e => e.WidthB).HasColumnType("decimal(18, 0)");

        builder.HasOne(d => d.BarCodeFormat).WithMany(p => p.BarCodeFormatDocumentTypeKeywords)
            .HasForeignKey(d => d.BarCodeFormatId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_BarCodeFormatDocumentType_BarCode");

        builder.HasOne(d => d.DocumentType).WithMany(p => p.BarCodeFormatDocumentTypeKeywords)
            .HasForeignKey(d => d.DocumentTypeId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_BarCodeDocumentType_DocumentType");

        builder.HasOne(d => d.Keyword).WithMany(p => p.BarCodeFormatDocumentTypeKeywords)
            .HasForeignKey(d => d.KeywordId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_BarCodeDocumentType_Keyword");

        builder.HasIndex(x => new { x.DocumentTypeId, x.BarCodeFormatId, x.KeywordId });
    }
}
