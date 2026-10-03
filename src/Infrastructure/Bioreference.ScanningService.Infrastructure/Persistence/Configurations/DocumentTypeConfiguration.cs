using Bioreference.ScanningService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioreference.ScanningService.Infrastructure.Persistence.Configurations;

public class DocumentTypeConfiguration : IEntityTypeConfiguration<DocumentType>
{
    public void Configure(EntityTypeBuilder<DocumentType> builder)
    {
        builder.ToTable("DocumentType");

        builder.HasKey(x => x.DocumentTypeId);
        builder.Property(x => x.DocumentTypeId).HasColumnName("DocumentTypeId");

        builder.Property(x => x.GroupId).HasColumnName("GroupId");

        builder.Property(x => x.Code)
            .HasColumnName("Code")
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(x => x.DisplayName)
            .HasColumnName("DisplayName")
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
        builder.Property(e => e.DocumentNameFormat)
           .HasMaxLength(50)
           .IsUnicode(false);
        builder.Property(e => e.DocumentNameShortFormat)
            .HasMaxLength(50)
            .IsUnicode(false);
        builder.HasOne(x => x.Group)
            .WithMany(x => x.DocumentTypes)
            .HasForeignKey(x => x.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.Code).IsUnique();
    }
}
