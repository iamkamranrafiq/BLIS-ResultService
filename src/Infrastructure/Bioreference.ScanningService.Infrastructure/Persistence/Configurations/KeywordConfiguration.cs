using Bioreference.ScanningService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioreference.ScanningService.Infrastructure.Persistence.Configurations;

public class KeywordConfiguration : IEntityTypeConfiguration<Keyword>
{
    public void Configure(EntityTypeBuilder<Keyword> builder)
    {
        builder.ToTable("Keyword");

        builder.HasKey(x => x.KeywordId);
        builder.Property(x => x.KeywordId).HasColumnName("KeywordId");

        builder.Property(x => x.Name)
            .HasColumnName("Name")
            .HasColumnType("varchar(255)")
            .IsRequired();

        builder.Property(x => x.DataType)
            .HasColumnName("DataType")
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(x => x.ControlType)
            .HasColumnName("ControlType")
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(x => x.IsRequired).HasColumnName("IsRequired");
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

        builder.Property(x => x.OnBaseTableType)
            .HasColumnName("OnBaseTableType");
    }
}
