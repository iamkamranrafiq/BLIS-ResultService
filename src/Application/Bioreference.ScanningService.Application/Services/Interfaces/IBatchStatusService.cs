using Bioreference.ScanningService.Application.DTOs.Requests;
using Bioreference.ScanningService.Application.DTOs.Responses;

namespace Bioreference.ScanningService.Application.Services.Interfaces;

public interface IBatchStatusService
{
    Task<GetBatchStatusesResponse> GetBatchStatusesAsync(GetBatchStatusesRequest request);
}
