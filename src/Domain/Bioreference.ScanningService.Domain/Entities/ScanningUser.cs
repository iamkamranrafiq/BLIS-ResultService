namespace Bioreference.ScanningService.Domain.Entities;

public class ScanningUser
{
    public long Id { get; set; }
    public Guid EntraObjectId { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ScanningUserGroupScanningUser> UserGroups { get; set; }
        = new List<ScanningUserGroupScanningUser>();
}
