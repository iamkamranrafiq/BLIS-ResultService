using Bioreference.ResultService.Application.Audit;
using Bioreference.ResultService.Application.Lookup;
using Bioreference.ResultService.Application.Order;
using Bioreference.ResultService.Application.Report;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Bioreference.ResultService.WebAPI.Controllers;
using Bioreference.Result.WebAPI.Controllers;
using Bioreference.ResultService.Application.RapidResult;
using Bioreference.ResultService.Abstractions.Application.Payload;
using Bioreference.ResultService.Application.Payload;
using Bioreference.ResultService.Abstractions.Application.ApiClient;
using Bioreference.ResultService.Application.ApiClient;
using Bioreference.ResultService.Application.Model.AppSettings.ApiClient;
using Bioreference.ResultService.Application.Model.AppSettings.QueueOrder;

namespace Bioreference.Result.WebAPI.Extensions
{
    public static class ResultServiceExtensions
    {
        public static IServiceCollection AddResultServiceDependencies(this IServiceCollection services, IConfiguration configuration = null)
        {

            var applicationAssembly = Assembly.Load("Bioreference.ResultService.Application");
            var abstractionsAssembly = Assembly.Load("Bioreference.ResultService.Abstractions");
            services.Configure<AppSettingsQueueOrderInProcessor>(configuration.GetSection(AppSettingsQueueOrderInProcessor.SectionName));
            services.Scan(scan => scan
                .FromAssemblies(applicationAssembly, abstractionsAssembly)
                .AddClasses(classes => classes.Where(c => c.Name.EndsWith("Service")))
                .AsImplementedInterfaces()
                .WithScopedLifetime());
            services.Scan(scan => scan
                .FromAssemblies(applicationAssembly, abstractionsAssembly)
                .AddClasses(classes => classes.Where(c => c.Name.EndsWith("Processor")))
                .AsImplementedInterfaces()
                .WithScopedLifetime());
            services.AddScoped<ResultEventProcessorService>();
            services.AddScoped<RapidEventProcessorService>();
            services.AddAutoMapper(_ => { },
                typeof(OrderService).Assembly,
                typeof(OrderSearchService).Assembly,
                typeof(ReportService).Assembly,
                typeof(AuditService).Assembly,
                typeof(LookupService).Assembly,
                typeof(WorkSheetService).Assembly,
                typeof(ReportController).Assembly,
                typeof(COCController).Assembly,
                typeof(LookupController).Assembly,
                typeof(OrderController).Assembly,
                typeof(RackWorkSheetController).Assembly,
                typeof(RapidResultController).Assembly,
                typeof(ToolsController).Assembly);
            
            // Add more services here
            services.AddScoped<ISSUPayloadService, SSUPayloadService>();
            services.AddScoped<IORUPayloadService, ORUPayloadService>();
            services.AddScoped<ISTSPayloadService, STSPayloadService>();
            
            // Register IPayloadSenderApiClient for processors that need it
            services.AddTransient<IPayloadSenderApiClient, PayloadSenderApiClient>();
            if (configuration != null)
            {
                services.Configure<AppSettingsPayloadSender>(configuration.GetSection(AppSettingsPayloadSender.SectionName));
            }
            
            return services;
        }

        public static void AddSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "Version : " + (typeof(Program).Assembly.GetName().Version?.ToString() ?? "Unknown"),
                    Title = (typeof(Program).Assembly.GetName().Name?.ToString() ?? "Unknown") + "(" + (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production") + ")",
                });
                c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());


                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter token like: Bearer {your JWT token}"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                             Reference = new OpenApiReference
                             {
                                 Type = ReferenceType.SecurityScheme,
                                 Id = "Bearer"
                              }
                        },
                    Array.Empty<string>()
                    }
                });
            });

            services.Configure<MvcOptions>(c => c.Conventions.Add(new SwaggerApplicationConvention()));
        }

        public static void AddCorsSetting(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader());
            });
        }

        public static void ConfigureSwagger(this IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
    }
}
