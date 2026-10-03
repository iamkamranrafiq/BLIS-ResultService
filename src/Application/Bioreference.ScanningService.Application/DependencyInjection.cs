using Bioreference.ScanningService.Application.Services.Implementations;
using Bioreference.ScanningService.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;


namespace Bioreference.ScanningService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IBatchService, BatchService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IScanQueueService, ScanQueueService>();
        services.AddScoped<IScanFormatService, ScanFormatService>();
        services.AddScoped<IBatchStatusService, BatchStatusService>();
        services.AddScoped<IScanStatsService, ScanStatsService>();
        services.AddScoped<IRequisitionService, RequisitionService>();
        services.AddScoped<IScanningUserService, ScanningUserService>();

        // Barcode processing
        services.AddSingleton<IImageAnnotator, SkiaSharpImageAnnotator>();
        services.AddSingleton<ZxingBarcodeDetector>();
        services.AddSingleton<BarcodeService>();

        return services;
    }
}
