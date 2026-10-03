using System.Diagnostics;

namespace Bioreference.ResultService.WebAPI.Middleware
{
    public class ApiRequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiRequestLoggingMiddleware> _logger;

        public ApiRequestLoggingMiddleware(RequestDelegate next, ILogger<ApiRequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var request = context?.Request;
            var endpoint = request?.Path.Value ?? "Unknown";
            var method = request?.Method ?? "Unknown";
            var controller = ResolveController(endpoint);

            _logger.LogDebug(string.Format(
                "Entity: {0}; Event: {1}; Message: {2}; Api: {3}; Method: {4};",
                controller,
                "ApiCalled",
                $"{controller} API invoked.",
                endpoint,
                method));

            var stopwatch = Stopwatch.StartNew();
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, string.Format(
                    "Entity: {0}; Event: {1}; Message: {2}; Api: {3}; Method: {4}; StatusCode: {5}; ElapsedTime: {6} ms; Error: {7};",
                    controller,
                    "Exception",
                    $"{controller} API failed with exception.",
                    endpoint,
                    method,
                    500,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message));
                throw;
            }

            stopwatch.Stop();
            var statusCode = context?.Response?.StatusCode;

            //uncomment if we need status code based logging

            //if (statusCode >= 400)
            //{
            //    var errorMessage = context?.Items["ApiErrorMessage"]?.ToString();
            //    if (string.IsNullOrWhiteSpace(errorMessage))
            //    {
            //        errorMessage = $"Request failed with status code {statusCode}";
            //    }

            //    _logger.LogError(string.Format(
            //        "Entity: {0}; Event: {1}; Message: {2}; Api: {3}; Method: {4}; StatusCode: {5}; ElapsedTime: {6} ms; Error: {7}",
            //        controller,
            //        "HttpError",
            //        $"{controller} API failed.",
            //        endpoint,
            //        method,
            //        statusCode,
            //        stopwatch.ElapsedMilliseconds,
            //        errorMessage));

            //    return;
            //}

            _logger.LogInformation(string.Format(
                "Entity: {0}; Event: {1}; Message: {2}; Api: {3}; Method: {4}; StatusCode: {5}; ElapsedTime: {6} ms",
                controller,
                "Success",
                $"{controller} API completed.",
                endpoint,
                method,
                statusCode,
                stopwatch.ElapsedMilliseconds));
        }

        private static string ResolveController(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return "Unknown";
            }

            var segment = endpoint.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return string.IsNullOrWhiteSpace(segment) ? "Unknown" : segment;
        }

        
    }
}
