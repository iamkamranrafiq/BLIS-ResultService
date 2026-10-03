using Bioreference.ScanningService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioreference.ScanningService.Infrastructure.Persistence.Configurations;

public class DocumentTypeGroupConfiguration : IEntityTypeConfiguration<DocumentTypeGroup>
{
    public void Configure(EntityTypeBuilder<DocumentTypeGroup> builder)
    {
        builder.ToTable("DocumentTypeGroup");

        builder.HasKey(x => x.DocumentTypeGroupId);
        builder.Property(x => x.DocumentTypeGroupId).HasColumnName("DocumentTypeGroupId");

        builder.Property(x => x.Name)
            .HasColumnName("Name")
            .HasColumnType("varchar(255)")
            .IsRequired();

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
