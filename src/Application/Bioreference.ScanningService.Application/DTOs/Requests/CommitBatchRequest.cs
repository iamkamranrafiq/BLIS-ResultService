using System.ComponentModel.DataAnnotations;

namespace Bioreference.ScanningService.Application.DTOs.Requests;

public class CommitBatchRequest
{
    [Required]
    public int BatchId { get; set; }
}
