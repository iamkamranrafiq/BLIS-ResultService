using Bioreference.ScanningService.Application.Common.Interfaces;
using Bioreference.ScanningService.Application.DTOs.Requests;
using Bioreference.ScanningService.Application.DTOs.Responses;
using Bioreference.ScanningService.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Bioreference.ScanningService.Application.Services.Implementations;

public class BatchStatusService : IBatchStatusService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BatchStatusService> _logger;

    public BatchStatusService(IUnitOfWork unitOfWork, ILogger<BatchStatusService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<GetBatchStatusesResponse> GetBatchStatusesAsync(GetBatchStatusesRequest request)
    {
        _logger.LogInformation("GetBatchStatusesAsync called - IsActive: {IsActive}", request.IsActive);

        try
        {
            var allStatuses = await _unitOfWork.BatchStatuses.GetAllAsync();

            // Optional IsActive filter
            if (request.IsActive.HasValue)
            {
                allStatuses = allStatuses.Where(s => s.IsActive == request.IsActive.Value).ToList();
            }

            var dtos = allStatuses.Select(s => new BatchStatusDto
            {
                BatchStatusId = s.BatchStatusId,
                Name = s.Name,
                IsActive = s.IsActive
            })
                .OrderBy(s => s.Name)
                .ToList();

            _logger.LogInformation("Returning {Count} batch statuses", dtos.Count);

            return new GetBatchStatusesResponse
            {
                Success = true,
                Message = "Batch statuses retrieved successfully.",
                Data = dtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving batch statuses");
            return new GetBatchStatusesResponse
            {
                Success = false,
                Message = "An error occurred while retrieving batch statuses."
            };
        }
    }
}
