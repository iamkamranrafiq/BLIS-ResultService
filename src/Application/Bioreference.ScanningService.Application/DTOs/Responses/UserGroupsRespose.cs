using System;
using System.Collections.Generic;
using System.Text;

namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class UserGroupsRespose
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public ICollection<UserGroup>? Data { get; set; }

}
