using System;
using System.Collections.Generic;
using System.Text;

namespace Bioreference.ScanningService.Application.DTOs.Responses;

public class ProductivityReportResponse : ApiResponse
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public List<ProductivityReport> Data { get; set; }
}

public class ProductivityReport
{
    public string UserName { get; set; }
    public DateTime Date { get; set; }
    public DateTime Hours { get; set; }
    public int Documents { get; set; }
    public int Pages { get; set; }
}





