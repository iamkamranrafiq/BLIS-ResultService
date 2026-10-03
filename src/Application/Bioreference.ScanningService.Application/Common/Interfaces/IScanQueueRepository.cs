using Bioreference.ScanningService.Application.DTOs.Requests;
using Bioreference.ScanningService.Application.DTOs.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bioreference.ScanningService.Application.Common.Interfaces;

public interface IScanQueueRepository
{
    public Task<List<ProductivityReport>> GetProductivityReport(GetScanProductivityReportRequest request);
}
