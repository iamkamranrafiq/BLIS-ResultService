using System;
using System.Collections.Generic;
using System.Text;

namespace Bioreference.ScanningService.Domain.Entities;

public class ScanningUserGroup
{
    public long Id { get; set; }
    public Guid EntraObjectId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ScanningUserGroupScanningUser> Users { get; set; }
        = new List<ScanningUserGroupScanningUser>();
}
