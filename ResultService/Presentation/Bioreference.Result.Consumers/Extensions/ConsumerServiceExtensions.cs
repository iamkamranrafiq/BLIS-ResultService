

using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Bioreference.ResultService.Extensions
{
    public static class ConsumerServiceExtensions
    {
        public static IServiceCollection AddConsumerServiceDependencies(this IServiceCollection services)
        {

            var applicationAssembly = Assembly.Load("Bioreference.ResultService.Application");
            var abstractionsAssembly = Assembly.Load("Bioreference.ResultService.Abstractions");

            services.Scan(scan => scan
                .FromAssemblies(applicationAssembly, abstractionsAssembly)
                .AddClasses(classes => classes.Where(c => c.Name.EndsWith("Processor")))
                .AsImplementedInterfaces()
                .WithScopedLifetime());
            services.Scan(scan => scan
            .FromAssemblies(applicationAssembly, abstractionsAssembly)
            .AddClasses(classes => classes.Where(c => c.Name.EndsWith("Service")))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

            // Add more services here
            return services;
        }       
    }
}
