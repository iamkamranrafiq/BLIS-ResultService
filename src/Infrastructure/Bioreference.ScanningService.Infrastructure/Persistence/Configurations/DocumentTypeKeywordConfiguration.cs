using Bioreference.ScanningService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioreference.ScanningService.Infrastructure.Persistence.Configurations;

public class DocumentTypeKeywordConfiguration : IEntityTypeConfiguration<DocumentTypeKeyword>
{
    public void Configure(EntityTypeBuilder<DocumentTypeKeyword> builder)
    {
        builder.ToTable("DocumentTypeKeyword");

        builder.HasKey(x => x.DocumentTypeKeywordId);
        builder.Property(x => x.DocumentTypeKeywordId).HasColumnName("DocumentTypeKeywordId");

        builder.Property(x => x.DocumentTypeId).HasColumnName("DocumentTypeId");
        builder.Property(x => x.KeywordId).HasColumnName("KeywordId");

        builder.Property(x => x.IsRequired).HasColumnName("IsRequired");
        builder.Property(x => x.IsActive).HasColumnName("IsActive");
        builder.Property(x => x.DisplayOrder).HasColumnName("DisplayOrder");

        builder.Property(x => x.CreatedBy)
            .HasColumnName("CreatedBy")
            .HasColumnType("varchar(255)")
            .IsRequired();

        builder.Property(x => x.CreatedDate)
            .HasColumnName("CreatedDate")
            .HasColumnType("datetime");

        builder.HasOne(x => x.DocumentType)
            .WithMany(x => x.DocumentTypeKeywords)
            .HasForeignKey(x => x.DocumentTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Keyword)
            .WithMany(x => x.DocumentTypeKeywords)
            .HasForeignKey(x => x.KeywordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
