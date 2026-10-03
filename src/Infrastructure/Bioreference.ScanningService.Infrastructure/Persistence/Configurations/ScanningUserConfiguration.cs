using Bioreference.ScanningService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioreference.ScanningService.Infrastructure.Persistence.Configurations;

public class ScanningUserConfiguration : IEntityTypeConfiguration<ScanningUser>
{

    public void Configure(EntityTypeBuilder<ScanningUser> builder)
    {
        builder.ToTable("ScanningUser");

        builder.Property(x => x.EntraObjectId)
            .HasColumnName("EntraObjectId")
            .HasColumnType("nvarchar(100)")
            .HasConversion(
                guid => guid.ToString(),
                value => Guid.Parse(value));

        builder.HasIndex(x => x.EntraObjectId)
            .IsUnique();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("CreatedAt")
            .HasColumnType("datetime");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("UpdatedAt")
            .HasColumnType("datetime");
    }
}
