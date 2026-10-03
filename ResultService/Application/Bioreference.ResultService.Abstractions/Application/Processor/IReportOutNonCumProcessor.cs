
using Bioreference.Contracts.Result;

namespace Bioreference.ResultService.Abstractions.Application.Processor
{
    public interface IReportOutNonCumProcessor
    {
        public Task OnReportOutNonCumMessage(ReportOutNonCum reportOut);
    }
}
