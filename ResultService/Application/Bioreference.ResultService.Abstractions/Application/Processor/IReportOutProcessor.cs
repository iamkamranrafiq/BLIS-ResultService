using Bioreference.Contracts.Result;

namespace Bioreference.ResultService.Abstractions.Application.Processor
{
    public interface IReportOutProcessor
    {
        public Task OnReportOutMessage(ReportOut reportOut);
    }
}
