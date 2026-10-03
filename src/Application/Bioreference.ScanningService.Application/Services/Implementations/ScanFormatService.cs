using Bioreference.ScanningService.Application.Common.Interfaces;
using Bioreference.ScanningService.Application.DTOs.Requests;
using Bioreference.ScanningService.Application.DTOs.Responses;
using Bioreference.ScanningService.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Bioreference.ScanningService.Application.Services.Implementations;

public class ScanFormatService : IScanFormatService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ScanFormatService> _logger;

    public ScanFormatService(IUnitOfWork unitOfWork, ILogger<ScanFormatService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GetScanFormatsResponse> GetScanFormatsAsync(GetScanFormatsRequest request)
    {
        _logger.LogInformation("GetScanFormatsAsync called - IsActive: {IsActive}", request.IsActive);

        try
        {
            var allFormats = await _unitOfWork.ScanningFormats.GetAllAsync();

            // Optional IsActive filter
            if (request.IsActive.HasValue)
            {
                allFormats = allFormats.Where(f => f.IsActive == request.IsActive.Value).ToList();
            }

            var dtos = allFormats.Select(f => new ScanFormatDto
            {
                ScanningFormatId = f.ScanningFormatId,
                Name = f.Name,
                Description = f.Description,
                IsActive = f.IsActive,
                CreatedDate = f.CreatedDate,
                UpdatedDate = f.UpdatedDate
            })
                .OrderBy(f => f.Name)
                .ToList();

            _logger.LogInformation("Returning {Count} scan formats", dtos.Count);

            return new GetScanFormatsResponse
            {
                Success = true,
                Message = "Scan formats retrieved successfully.",
                Data = dtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scan formats");
            return new GetScanFormatsResponse
            {
                Success = false,
                Message = "An error occurred while retrieving scan formats."
            };
        }
    }
}
