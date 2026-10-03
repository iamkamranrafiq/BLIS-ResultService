using Bioreference.Contracts.Result;
using Bioreference.Logging;
using Bioreference.Messaging.Abstractions.DTO;
using Bioreference.ResultService.Abstractions.Application.Processor;
using Bioreference.ResultService.Application.Processor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bioreference.ResultService.Consumers.Test.Inbound
{
    [TestFixture]
    public class AuditTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void InboundAudit_Test()
        {
            var builder = new HostBuilder()
           .ConfigureServices((hostContext, services) =>
           {
               services.AddBioreferenceLogging("Bioreference.ResultService.Processor");
               services.AddSingleton<IInboundAuditProcessor, InboundAuditProcessor>();
           });
            var message = new MessageContext<AuditOut>
            {
                Message = new Message<AuditOut>
                {
                    Payload = new AuditOut
                    {
                        Message = "07/07/2025 09:10:15.457|1|7079280113|Bioreference.LIS.ReportAnalyte|brli_dinj6p_results_in_1||1837967-07032025|1|37354791|0|InstrumentId^^GSD AIX 1000 RPR 215^System.String^False~ResultValue^^Non-Reactive^System.String^True~ResultDate^1/1/1900^12:00:00 AM^System.DateTime^False~ResultStatus^0^1^Bioreference.LIS.resultStatusType^False~PriorEUIDResultValue^^N/A^System.String^False~DeltaHoldRule^^N/A^System.String^False\r\n"
                    }
                }
            };

            var host = builder.Build();
            var logger = host.Services.GetRequiredService<ILogger<Audit>>();
            var inboundAuditProcessor = host.Services.GetRequiredService<IInboundAuditProcessor>();
            var inboundAuditConsumer = new Audit(logger, inboundAuditProcessor);

            var result = inboundAuditConsumer.ProcessMessage(message);

        }

    }
}