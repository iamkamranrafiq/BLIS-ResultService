using Bioreference.ScanningService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioreference.ScanningService.Infrastructure.Persistence.Configurations;

public class BarCodeFormatConfiguration : IEntityTypeConfiguration<BarCodeFormat>
{
    public void Configure(EntityTypeBuilder<BarCodeFormat> builder)
    {
        builder.ToTable("BarCodeFormat");

        builder.HasKey(x => x.BarCodeFormatId);
        builder.Property(x => x.BarCodeFormatId)
            .HasColumnName("BarCodeFormatId")
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .HasColumnName("Name")
            .HasColumnType("nvarchar(50)")
            .IsRequired(false);

        builder.Property(x => x.Length)
            .HasColumnName("Length")
            .IsRequired(false);

        builder.Property(x => x.SearchDirection)
            .HasColumnName("SearchDirection")
            .HasColumnType("varchar(50)")
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

        builder.Property(x => x.IsDeleted)
            .HasColumnName("IsDeleted")
            .IsRequired(false);
    }
}
