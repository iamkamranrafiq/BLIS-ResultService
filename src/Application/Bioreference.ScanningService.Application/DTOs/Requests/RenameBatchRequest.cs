using System.ComponentModel.DataAnnotations;

namespace Bioreference.ScanningService.Application.DTOs.Requests;

public class RenameBatchRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public string? UpdatedBy { get; set; }
}
