using Bioreference.ScanningService.Application.Common.Interfaces;
using Bioreference.ScanningService.Application.DTOs.Requests;
using Bioreference.ScanningService.Application.DTOs.Responses;
using Bioreference.ScanningService.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Bioreference.ScanningService.Application.Services.Implementations;

public class ScanQueueService : IScanQueueService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ScanQueueService> _logger;
    private readonly IScanQueueRepository _scanQueueRepository;

    public ScanQueueService(IUnitOfWork unitOfWork, ILogger<ScanQueueService> logger, IScanQueueRepository scanQueueRepository)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _scanQueueRepository = scanQueueRepository;
    }

    public async Task<GetScanQueuesResponse> GetScanQueuesAsync(GetScanQueuesRequest request)
    {
        _logger.LogInformation("GetScanQueuesAsync called - IsActive: {IsActive}", request.IsActive);

        try
        {
            var allQueues = await _unitOfWork.ScanQueues.GetAllAsync();

            // Filter out soft-deleted records
            allQueues = allQueues.Where(q => !q.IsDeleted).ToList();

            // Optional IsActive filter
            if (request.IsActive.HasValue)
            {
                allQueues = allQueues.Where(q => q.IsActive == request.IsActive.Value).ToList();
            }

            var dtos = allQueues.Select(q => new ScanQueueDto
            {
                ScanQueueId = q.ScanQueueId,
                Name = q.Name,
                IsAutoCommit = q.IsAutoCommit,
                LastUsedDate = q.LastUsedDate,
                IsActive = q.IsActive,
                CreatedDate = q.CreatedDate,
                UpdatedDate = q.UpdatedDate
            })
                .OrderBy(q => q.Name)
                .ToList();

            _logger.LogInformation("Returning {Count} scan queues", dtos.Count);

            return new GetScanQueuesResponse
            {
                Success = true,
                Message = "Scan queues retrieved successfully.",
                Data = dtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scan queues");
            return new GetScanQueuesResponse
            {
                Success = false,
                Message = "An error occurred while retrieving scan queues."
            };
        }
    }

    public async Task<ProductivityReportResponse> GetProductivityReport(GetScanProductivityReportRequest request)
    {
        _logger.LogInformation("GetProductivityReport called ");
        try
        {
            var response = await _scanQueueRepository.GetProductivityReport(request);
            return new ProductivityReportResponse()
            {
                Data = response,
                Success = true,
                Message = "Successfully getting the report data"
            };
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Getting error on fetching data from SP");
            return new ProductivityReportResponse()
            {
                Data = null,
                Success = false,
                Message = ex.Message
            };
        }
    }
}
