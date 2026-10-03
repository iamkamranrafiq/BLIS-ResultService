using System.ComponentModel.DataAnnotations;

namespace Bioreference.ScanningService.Application.DTOs.Requests;

public class RenameDocumentRequest
{
    [Required]
    [StringLength(255, MinimumLength = 1, ErrorMessage = "Document name must be between 1 and 255 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "UpdatedBy cannot exceed 255 characters.")]
    public string? UpdatedBy { get; set; }
}
