using Bioreference.Logging;
using Bioreference.Result.WebAPI.Extensions;
using Bioreference.ResultService.WebAPI.Middleware;
using Bioreference.Contracts.Result;
using Bioreference.Messaging.Kafka;
using Bioreference.ResultService.WebAPI.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddNewtonsoftJson();
builder.Services.RegisterProducer<Incident>();
builder.Services.RegisterProducer<Reflex>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddBioreferenceLogging("Result.WebAPI");
builder.Services.AddSwagger();
builder.Services.AddResultServiceDependencies(builder.Configuration);
builder.Services.AddCorsSetting();
builder.Services.AddHealthChecks();
builder.Services.AddHostedService<DomainLoggingInitializer>();
builder.Services.RegisterProducer<AuditOut>();
var app = builder.Build();

// Configure the HTTP request pipeline.
// Add header logging middleware (controlled by ENABLE_HEADER_LOGGING=true env var)
app.UseMiddleware<HeaderLoggingMiddleware>();
app.UseMiddleware<IdentityMiddleware>();
app.UseMiddleware<ApiRequestLoggingMiddleware>();
//app.EnforceBioreferenceLogKey();
if (!app.Environment.IsProduction())
{
    app.ConfigureSwagger();
}
app.UseCors("CorsPolicy");
app.UseAuthorization();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();