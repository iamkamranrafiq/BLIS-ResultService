using Bioreference.ScanningService.Application;
using Bioreference.ScanningService.Application.Common.Settings;
using Bioreference.ScanningService.Infrastructure;
using Bioreference.ScanningService.Infrastructure.Security;
using Bioreference.ScanningService.WinService.Authentication;
using Bioreference.ScanningService.WinService.Services.Interfaces;
using Bioreference.ScanningService.WinService.Hubs;
using Bioreference.ScanningService.WinService.Settings;
using Bioreference.ScanningService.WinService.Workers;
using Serilog;
using Serilog.Events;
using Bioreference.ScanningService.WinService.Services.Implementations;
using Bioreference.ScanningService.Application.Common.Interfaces;
using Bioreference.ScanningService.Infrastructure.Storage;
using Bioreference.ScanningService.WinService.Services;

if (TwainWorkerProcess.IsWorker(args))
{
    return await TwainWorkerProcess.RunAsync(args);
}

// ── Secret encryption helper ─────────────────────────────────────────────────────────────────
// Usage: Bioreference.ScanningService.WinService.exe encrypt-secret "<plain text>"
// Prints the ENC:... value to paste into appsettings.json, then exits.
if (args.Length >= 1 && string.Equals(args[0], "encrypt-secret", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Usage: encrypt-secret \"<plain text to encrypt>\"");
        return 1;
    }

    Console.WriteLine(SecretProtector.Encrypt(args[1]));
    return 0;
}

// ── Secret encryption helper ─────────────────────────────────────────────────────────────────
// Usage: Bioreference.ScanningService.WinService.exe encrypt-secret "<plain text>"
// Prints the ENC:... value to paste into appsettings.json, then exits.
if (args.Length >= 1 && string.Equals(args[0], "encrypt-secret", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Usage: encrypt-secret \"<plain text to encrypt>\"");
        return 1;
    }

    Console.WriteLine(SecretProtector.Encrypt(args[1]));
    return 0;
}

var logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
Directory.CreateDirectory(logsDirectory);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: Path.Combine(logsDirectory, "winservice-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        shared: true,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("BLIS Scanning WinService starting");

    var builder = WebApplication.CreateBuilder(args);

    // Transparently decrypt ENC:-prefixed secrets (connection string, share credentials)
    // so the rest of the app reads plain configuration values.
    builder.Configuration.AddDecryptedSecrets();

    // ── Windows Service integration ───────────────────────────────────────────
    builder.Host.UseWindowsService(options =>
    {
        options.ServiceName = "BLIS Scanner Service";
    });
    builder.Host.UseSerilog();

    // ── REST API ──────────────────────────────────────────────────────────────
    builder.Services.AddControllers();

    // ── SignalR ───────────────────────────────────────────────────────────────
    builder.Services.AddSignalR(options =>
    {
        // Allow pause/resume/stop hub methods to run concurrently with the long-running
        // StartScanProcess invocation on the same connection. With the default value of 1,
        // those control calls are queued behind the active scan and only run after it finishes.
        options.MaximumParallelInvocationsPerClient = 4;
    });

    // ── CORS ──────────────────────────────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy => policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
    });

    // ── Clean Architecture layers ─────────────────────────────────────────────
    builder.Services.AddApplication();
    builder.Services.AddSingleton<IRequestUserContext, WindowsServiceRequestUserContext>();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddScoped<IScannerService, TwainScanService>();

    // Azure AD JWT authentication: verify the same bearer tokens the BLIS Angular UI sends.
    builder.Services.AddScanningAuthentication(builder.Configuration);

    // ── Path converter (OS-aware) ─────────────────────────────────────────────
    if (OperatingSystem.IsLinux())
        builder.Services.AddSingleton<IPathConverter, LinuxPathConverter>();
    else
        builder.Services.AddSingleton<IPathConverter, WindowsPathConverter>();

    // ── Scanner settings
    builder.Services.Configure<ScannerSettings>(
        builder.Configuration.GetSection(ScannerSettings.SectionName));

    // ── Barcode settings ──────────────────────────────────────────────────────
    builder.Services.Configure<BarcodeSettings>(
        builder.Configuration.GetSection(BarcodeSettings.SectionName));

    // ── Test settings ─────────────────────────────────────────────────────────
    builder.Services.Configure<TestSettings>(
        builder.Configuration.GetSection(TestSettings.SectionName));

    // ── Background worker ─────────────────────────────────────────────────────
    //builder.Services.AddHostedService<ScanningWorker>();

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    app.UseCors("AllowAll");
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();

    // ── Route mappings ────────────────────────────────────────────────────────
    app.MapControllers();
    app.MapHub<ScannerHub>("/hubs/scanner");

    Log.Information("BLIS Scanning WinService running");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "BLIS Scanning WinService terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;
