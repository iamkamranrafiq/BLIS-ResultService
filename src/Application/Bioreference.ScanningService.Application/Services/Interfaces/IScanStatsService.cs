using Bioreference.ScanningService.Application.DTOs.Requests;
using Bioreference.ScanningService.Application.DTOs.Responses;

namespace Bioreference.ScanningService.Application.Services.Interfaces;

public interface IScanStatsService
{


    Task<ScanDashboardStatsResponse> GetDashboardStatsAsync();

    Task<GetLastCompletedByQueueResponse> GetLastCompletedByQueueAsync(GetLastCompletedByQueueRequest request);
}
