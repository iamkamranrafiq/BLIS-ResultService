using Bioreference.ScanningService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bioreference.ScanningService.Infrastructure.Persistence.Configurations;

public class ScanningUserGroupScanningUserConfiguration : IEntityTypeConfiguration<ScanningUserGroupScanningUser>
{
    public void Configure(EntityTypeBuilder<ScanningUserGroupScanningUser> builder)
    {
        builder.ToTable("ScanningUserGroupScanningUser");

        builder.HasKey(x => new
        {
            x.UserId,
            x.UserGroupId
        });

        builder.HasOne(x => x.User)
                .WithMany(x => x.UserGroups)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.UserGroup)
        .WithMany(x => x.Users)
        .HasForeignKey(x => x.UserGroupId);
    
    }
}
