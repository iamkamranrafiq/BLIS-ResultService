namespace Bioreference.ScanningService.Domain.Entities;

public class ScanningUserGroupScanningUser
{
    public long UserId { get; set; }

    public long UserGroupId { get; set; }

    public DateTime CreatedAt { get; set; }
    public virtual ScanningUserGroup UserGroup { get; set; } = null!;
    public virtual ScanningUser User { get; set; } = null!;
}
