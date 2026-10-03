using System.ComponentModel.DataAnnotations;

namespace Bioreference.ScanningService.Application.DTOs.Requests;

public class DeleteBatchesRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one batch ID must be provided.")]
    public List<int> BatchIds { get; set; } = new();

    [StringLength(255, ErrorMessage = "DeletedBy cannot exceed 255 characters.")]
    public string? DeletedBy { get; set; }
}
