using Bioreference.ScanningService.Application.Common.Interfaces;
using Bioreference.ScanningService.Application.DTOs.Requests;
using Bioreference.ScanningService.Application.DTOs.Responses;
using Bioreference.ScanningService.Application.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Bioreference.ScanningService.Application.Services.Implementations;

public class ScanStatsService : IScanStatsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<ScanStatsService> _logger;

    public ScanStatsService(IUnitOfWork unitOfWork, IFileStorage fileStorage, ILogger<ScanStatsService> logger)
    {
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    

    public async Task<ScanDashboardStatsResponse> GetDashboardStatsAsync()
    {
        _logger.LogInformation("GetDashboardStatsAsync called");

        try
        {
            // Rolling 30-day window (inclusive of now).
            var periodEnd = DateTime.UtcNow;
            var periodStart = periodEnd.AddDays(-30);

            // Only include rows where IsDeleted = 0 AND CreatedDate falls within the last 30 days.
            var documents = (await _unitOfWork.Documents.GetAllAsync())
                .Where(d => d.IsDeleted == false)
                .Where(d => d.CreatedDate >= periodStart && d.CreatedDate <= periodEnd)
                .ToList();

            var documentIds = documents.Select(d => d.DocumentId).ToHashSet();

            var documentPages = (await _unitOfWork.DocumentPages.GetAllAsync())
                .Where(p => p.IsDeleted == false)
                .Where(p => p.DocumentId.HasValue && documentIds.Contains(p.DocumentId.Value))
                .ToList();

            var totalDocumentsScanned = documents.Count;
            var totalPagesScanned = documentPages.Count;

            var response = new ScanDashboardStatsResponse
            {
                Success = true,
                Message = "Dashboard stats retrieved successfully.",
                MonthlyPagesScanned = totalPagesScanned,
                MonthlyDocumentsScanned = totalDocumentsScanned,
             
            };

            _logger.LogInformation("Dashboard stats - Pages: {Pages}, Documents: {Docs}",
                response.MonthlyPagesScanned, response.MonthlyDocumentsScanned);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard stats");
            return new ScanDashboardStatsResponse
            {
                Success = false,
                Message = "An error occurred while retrieving dashboard stats."
            };
        }
    }

    public async Task<GetLastCompletedByQueueResponse> GetLastCompletedByQueueAsync(GetLastCompletedByQueueRequest request)
    {
        _logger.LogInformation("GetLastCompletedByQueueAsync called - QueueIds: {Ids}",
            request?.ScanQueueIds is null ? "(null)" : string.Join(",", request.ScanQueueIds));

        try
        {
            var queueIds = (request?.ScanQueueIds ?? new List<int>())
                .Distinct()
                .ToList();

            if (queueIds.Count == 0)
            {
                return new GetLastCompletedByQueueResponse
                {
                    Success = true,
                    Message = "No scan queue ids provided.",
                    Data = new List<ScanQueueLastCompletedDto>()
                };
            }

            var lastCompletedByQueue = await _unitOfWork.Batches
                .GetLastCompletedDatesByQueueAsync(queueIds);

            var data = queueIds
                .Select(id => new ScanQueueLastCompletedDto
                {
                    ScanQueueId = id,
                    LastCompletedDate = lastCompletedByQueue.TryGetValue(id, out var d)
                        ? d.ToString("o")
                        : "-"
                })
                .ToList();

            return new GetLastCompletedByQueueResponse
            {
                Success = true,
                Message = "Last completed dates retrieved successfully.",
                Data = data
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving last completed dates by queue");
            return new GetLastCompletedByQueueResponse
            {
                Success = false,
                Message = "An error occurred while retrieving last completed dates."
            };
        }
    }
}
