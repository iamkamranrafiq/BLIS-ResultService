using System.Net;
using System.Text.Json;


namespace Bioreference.ResultService.WebAPI.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                //uncomment if you want status code based logging with APiRequestLoggingMiddleware
                
                //context.Items["ApiErrorMessage"] = ex.Message;
                _logger.LogError(ex, "Unhandled exception occurred");

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                var traceId = Guid.NewGuid();

                var response = new
                {
                   
                    statusCode = context.Response.StatusCode,
                    message = ex.Message,
                    details = ex.StackTrace,
                    traceId = traceId,
                    occurredAt = DateTime.UtcNow.ToString("o")
                };

                
                _logger.LogError($"Error occure while processing the request, TraceId : ${traceId}," +
                   $" Message : ${ex.Message}, StackTrace: ${ex.StackTrace}");
                
                var json = JsonSerializer.Serialize(response);
                await context.Response.WriteAsync(json);
            }
        }
    }

}
