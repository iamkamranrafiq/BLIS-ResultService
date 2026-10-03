using System;
using System.Collections.Generic;
using System.Text;

namespace Bioreference.ScanningService.Application.DTOs.Responses
{
    public class ApiResponse
    {
        public ApiResponseDetail ResponseStatus { get; set; } = new ApiResponseDetail();
    }
    public class ApiResponseDetail
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
    }
}
