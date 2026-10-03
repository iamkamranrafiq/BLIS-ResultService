using Bioreference.ScanningService.Application.DTOs.Requests;
using Bioreference.ScanningService.Application.DTOs.Responses;

namespace Bioreference.ScanningService.Application.Services.Interfaces;

public interface IScanFormatService
{
    Task<GetScanFormatsResponse> GetScanFormatsAsync(GetScanFormatsRequest request);
}
