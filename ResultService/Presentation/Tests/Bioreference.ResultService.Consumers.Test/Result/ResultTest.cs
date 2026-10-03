using Bioreference.Logging;
using Bioreference.ResultService.Abstractions.Application.Processor;
using Bioreference.ResultService.Abstractions.Application.Setting;
using Bioreference.ResultService.Application.Processor;
using Bioreference.ResultService.Application.Setting;
using Bioreference.ResultService.Processors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Bioreference.ResultService.Processors.Test.Inbound
{
    [TestFixture]
    public class ResultTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task Test1()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string FilePath = Path.Combine(baseDirectory, "Inbound", "HL7_InboundResult.txt");
            if (!File.Exists(FilePath))
            {
                throw new FileNotFoundException("File Path not found.", FilePath);
            }
            string file = File.ReadAllText(FilePath);
            string HL7string = file.Replace("\r\n", "\r");
            var builder = new HostBuilder()
           .ConfigureServices((hostContext, services) =>
           {
               services.AddBioreferenceLogging("Bioreference.ResultService.Processor");
               services.AddSingleton<ISettingService, SettingService>();
               services.AddSingleton<IInboundProcessor, InboundProcessor>();
           });
            var host = builder.Build();
            var logger = host.Services.GetRequiredService<ILogger<Bioreference.ResultService.Consumers.Result>>();
            var inboundProcessor = host.Services.GetRequiredService<IInboundProcessor>();
            //var inboundConsumer = new InboundConsumer(logger, inboundProcessor);
            //await Task.Run(() => inboundConsumer.ProcessInbound(HL7string));

        }

    }
}