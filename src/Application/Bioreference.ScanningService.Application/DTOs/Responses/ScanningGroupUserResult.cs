namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class ScanningGroupUserResult
{
    public long UserId { get; set; }
    public string? UserName { get; set; }
    public ICollection<UserGroup>? Groups { get; set; }
}

public class UserGroup
{
    public long GroupId { get; set; }
    public string? GroupName { get; set; }
}
