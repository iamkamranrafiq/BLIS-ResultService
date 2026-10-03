using Bioreference.ScanningService.Application.DTOs.Requests;
using Bioreference.ScanningService.Application.DTOs.Responses;

namespace Bioreference.ScanningService.Application.Services.Interfaces;

public interface IScanQueueService
{
    Task<GetScanQueuesResponse> GetScanQueuesAsync(GetScanQueuesRequest request);
    Task<ProductivityReportResponse> GetProductivityReport(GetScanProductivityReportRequest request);
}
