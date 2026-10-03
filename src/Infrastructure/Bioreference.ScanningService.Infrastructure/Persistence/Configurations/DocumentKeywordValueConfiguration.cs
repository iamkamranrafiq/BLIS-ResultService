using Bioreference.ScanningService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioreference.ScanningService.Infrastructure.Persistence.Configurations;

public class DocumentKeywordValueConfiguration : IEntityTypeConfiguration<DocumentKeywordValue>
{
    public void Configure(EntityTypeBuilder<DocumentKeywordValue> builder)
    {
        builder.ToTable("DocumentKeywordValue");

        builder.HasKey(x => x.DocumentKeywordValueId);
        builder.Property(x => x.DocumentKeywordValueId).HasColumnName("DocumentKeywordValueId");

        builder.Property(x => x.DocumentId).HasColumnName("DocumentId");
        builder.Property(x => x.KeywordId).HasColumnName("KeywordId");

        builder.Property(x => x.AlphanumericValue)
            .HasColumnName("AlphanumericValue")
            .HasColumnType("varchar(1000)");

        builder.Property(x => x.DateTimeValue)
            .HasColumnName("DateTimeValue")
            .HasColumnType("datetime");

        builder.Property(x => x.DecimalValue)
            .HasColumnName("DecimalValue")
            .HasColumnType("decimal(18, 4)");

        builder.Property(x => x.LongValue)
            .HasColumnName("LongValue")
            .HasColumnType("bigint");

        builder.Property(x => x.TextValue)
            .HasColumnName("TextValue")
            .HasColumnType("varchar(max)");

        builder.Property(x => x.Sequence)
            .HasColumnName("Sequence");

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

        builder.HasOne(x => x.Document)
            .WithMany(x => x.DocumentKeywordValues)
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Keyword)
            .WithMany(x => x.DocumentKeywordValues)
            .HasForeignKey(x => x.KeywordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
