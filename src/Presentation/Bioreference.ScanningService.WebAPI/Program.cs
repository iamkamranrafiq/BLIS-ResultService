using Bioreference.Logging;
using Bioreference.ScanningService.Application;
using Bioreference.ScanningService.Application.Common.Constants;
using Bioreference.ScanningService.Application.Common.Settings;
using Bioreference.ScanningService.Infrastructure;
using Bioreference.ScanningService.Infrastructure.Security;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Bioreference.ScanningService.Application.Common.Interfaces;
using Bioreference.ScanningService.Infrastructure.Storage;
using Bioreference.ScanningService.WebAPI.Authentication;
using Bioreference.ScanningService.WebAPI.Services;

var logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
Directory.CreateDirectory(logsDirectory);

try
{

    var builder = WebApplication.CreateBuilder(args);

    // Transparently decrypt ENC:-prefixed secrets (connection string, share credentials)
    // so the rest of the app reads plain configuration values.
    builder.Configuration.AddDecryptedSecrets();

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddSingleton<IRequestUserContext, RequestUserContext>();

    // Azure AD (Entra ID) JWT bearer authentication using the AzureAd config section.
    builder.Services.AddScanningAuthentication(builder.Configuration);

    // Add Swagger/OpenAPI support
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddBioreferenceLogging("Scanning.WebAPI");
    builder.Services.AddSwaggerGen();

    // Add CORS if needed
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll",
            builder => builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());
    });

    // Compose Clean Architecture layers
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddInfrastructureHealthChecks(builder.Configuration);

    // ── Path converter (OS-aware) ─────────────────────────────────────────────
    if (OperatingSystem.IsLinux())
        builder.Services.AddSingleton<IPathConverter, LinuxPathConverter>();
    else
        builder.Services.AddSingleton<IPathConverter, WindowsPathConverter>();

    // ── Barcode settings
    builder.Services.Configure<BarcodeSettings>(
        builder.Configuration.GetSection(BarcodeSettings.SectionName));
    // ────────────────── OnBase configuration ──────────────────
    builder.Services.Configure<OnBaseConfiguration>(
        builder.Configuration.GetSection(OnBaseConfiguration.DataSource));

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (!app.Environment.IsProduction())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "BLIS Scanning Service API v1");
            options.RoutePrefix = "swagger"; // Access at /swagger
        });
    }

    app.UseHttpsRedirection();
    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResultStatusCodes =
        {
            [HealthStatus.Healthy] = StatusCodes.Status200OK,
            [HealthStatus.Degraded] = StatusCodes.Status200OK,
            [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
        },
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                status = report.Status.ToString(),
                totalDurationMs = report.TotalDuration.TotalMilliseconds,
                checks = report.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    durationMs = entry.Value.Duration.TotalMilliseconds
                })
            });
        }
    });
    app.MapControllers();

    // Log application startup
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Starting {ApplicationName} v{ApplicationVersion}",
        AppConstants.ApplicationName,
        AppConstants.ApplicationVersion);

    app.Run();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[FATAL] Application startup failed: {ex}");
    var startupLog = Path.Combine(logsDirectory, "startup-error.log");
    File.AppendAllText(startupLog, $"{DateTime.UtcNow:o} {ex}{Environment.NewLine}");
    throw;
}
finally
{
    // No need to close and flush Serilog since we're using BioReference.Logging
}
