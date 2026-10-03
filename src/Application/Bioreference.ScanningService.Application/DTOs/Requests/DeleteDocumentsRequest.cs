using System.ComponentModel.DataAnnotations;

namespace Bioreference.ScanningService.Application.DTOs.Requests;

public class DeleteDocumentsRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "At least one document ID must be provided.")]
    public List<long> DocumentIds { get; set; } = new();

    [StringLength(255, ErrorMessage = "DeletedBy cannot exceed 255 characters.")]
    public string? DeletedBy { get; set; }
}
