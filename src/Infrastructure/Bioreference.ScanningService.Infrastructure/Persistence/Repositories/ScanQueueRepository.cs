using Bioreference.ScanningService.Application.Common.Interfaces;
using Bioreference.ScanningService.Application.DTOs.Requests;
using Bioreference.ScanningService.Application.DTOs.Responses;
using Bioreference.ScanningService.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bioreference.ScanningService.Infrastructure.Persistence.Repositories;

public class ScanQueueRepository : Repository<ScanQueue>, IScanQueueRepository
{
    private readonly ILogger<ScanQueueRepository> _logger;

    public ScanQueueRepository(ScanningServiceDbContext context, ILogger<ScanQueueRepository> logger) : base(context)
    {
        _logger = logger;
    }

    public async Task<List<ProductivityReport>> GetProductivityReport(GetScanProductivityReportRequest request)
    {
        _logger.LogInformation("GetProductivityReport called");
        try
        {
            var users = request.UserNames != null && request.UserNames.Any()  ? string.Join(",", request.UserNames) : null;

            var baseParameters = new[] {
                                        new SqlParameter("@StartDateTime", (object?)request.StartDateTime ?? DBNull.Value),
                                        new SqlParameter("@EndDateTime", (object?)request.EndDateTime ?? DBNull.Value),
                                        new SqlParameter("@StartTime", (object?)request.StartTime ?? DBNull.Value),
                                        new SqlParameter("@EndTime", (object?)request.EndTime ?? DBNull.Value),
                                        new SqlParameter("@UserNames", request.UserNames?.Any() == true ? string.Join(",", request.UserNames) : DBNull.Value)
            };

            return await _context.Database.SqlQueryRaw<ProductivityReport>(
                                                @"EXEC dbo.sp_GetProductivityReport @StartDateTime, @EndDateTime, @StartTime, @EndTime, @UserNames", baseParameters.ToArray())
                                            .AsNoTracking()
                                            .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, @"Error executing sp_ProductivtyReport with message: {0}", ex.Message);
            throw;
        }
    }
}
