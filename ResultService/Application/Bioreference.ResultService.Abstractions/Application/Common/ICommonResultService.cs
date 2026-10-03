using Bioreference.Common.Lab;
using Bioreference.Common;
using Bioreference.ResultService.Application.Model;

namespace Bioreference.ResultService.Abstractions.Application.Common
{
    public interface ICommonResultService
    {
        public Task ProduceAuditOutMessage(string message);
        public ReportModel UpdateResultFlagRangeInReportModel(ReportModel mappedReport, Bioreference.LIS.Report report);
        public void UpdateIsBlindInReport(Bioreference.LIS.Report report);
        public ComplexResultFlag GetMatchingResultFlag(ComplexResultFlag[] resultFlagRanges, Gender gender, string dob, int ageNbr, string ageType, DateTime calcDobFromDate = default, string performingFacility = "");
    }
}
