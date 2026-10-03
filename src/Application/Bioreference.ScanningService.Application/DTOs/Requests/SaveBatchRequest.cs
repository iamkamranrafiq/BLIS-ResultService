using System.ComponentModel.DataAnnotations;

namespace Bioreference.ScanningService.Application.DTOs.Requests;

public class SaveBatchRequest
{
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    public int? ScanQueueId { get; set; }

    [Range(1, int.MaxValue)]
    public int ScannerId { get; set; }

    public int? ScanningFormatId { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string ScanMode { get; set; } = string.Empty;

    public int? BatchStatusId { get; set; }

    public int TotalDocuments { get; set; }

    public int TotalPages { get; set; }

    public DateTime? StartedDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    public bool IsActive { get; set; } = true;
}
