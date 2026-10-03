using Microsoft.Extensions.Hosting;
using Bioreference.Messaging.Kafka;
using Microsoft.Extensions.Configuration;
using Bioreference.ResultService.Extensions;
using Bioreference.ResultService.Consumers.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Bioreference.Logging;
using Bioreference.ResultService.Application.Model.AppSettings.Logging;


var builder = new HostBuilder() 
    .ConfigureServices((hostContext, services) =>
    {
        //Register Consumer
        services.AddHostedService<IdentityInitializer>();
        services.AddHostedService<DomainLoggingInitializer>();
        services.RegisterConsumer();
        services.AddConsumerServiceDependencies();

        var serviceProvider = services.BuildServiceProvider();
        IConfiguration configuration = serviceProvider.GetRequiredService<IConfiguration>();

        var consumerLogSettings = new LogSettings();
        configuration.GetSection(LogSettings.SectionConsumerLogging).Bind(consumerLogSettings);
        services.AddBioreferenceLogging(consumerLogSettings.LoggingName);
    
    })
    .UseConsoleLifetime()
    .Build();
builder.UseConsumer();
await builder.RunAsync();
