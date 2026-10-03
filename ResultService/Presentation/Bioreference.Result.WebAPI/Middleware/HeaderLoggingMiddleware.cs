using System.Text;

namespace Bioreference.ResultService.WebAPI.Middleware
{
    public class HeaderLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<HeaderLoggingMiddleware> _logger;

        public HeaderLoggingMiddleware(RequestDelegate next, ILogger<HeaderLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only log if enabled via environment variable
            var enableHeaderLogging = Environment.GetEnvironmentVariable("ENABLE_HEADER_LOGGING");
            
            if (!string.IsNullOrEmpty(enableHeaderLogging) && 
                enableHeaderLogging.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                LogRequestHeaders(context);
            }

            await _next(context);
        }

        private void LogRequestHeaders(HttpContext context)
        {
            var headers = context.Request.Headers;
            var sb = new StringBuilder();
            sb.AppendLine($"Request Headers for {context.Request.Method} {context.Request.Path}:");
            
            int totalSize = 0;
            foreach (var header in headers)
            {
                var headerSize = header.Key.Length + string.Join("", header.Value).Length;
                totalSize += headerSize;
                sb.AppendLine($"  {header.Key}: {header.Value} (Size: {headerSize} bytes)");
            }
            
            sb.AppendLine($"Total Headers Size: {totalSize} bytes");
            sb.AppendLine($"Total Headers Count: {headers.Count}");
            
            _logger.LogInformation(sb.ToString());
        }
    }
}

