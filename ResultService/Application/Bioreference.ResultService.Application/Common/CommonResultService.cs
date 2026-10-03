using AutoMapper;
using Bioreference.Common;
using Bioreference.Common.Lab;
using Bioreference.Contracts.Result;
using Bioreference.LIS;
using Bioreference.Messaging.Abstractions;
using Bioreference.Messaging.Abstractions.DTO;
using Bioreference.ResultService.Abstractions.Application.Common;
using Bioreference.ResultService.Application.Model;

namespace Bioreference.ResultService.Application.Common
{
    public class CommonResultService: ICommonResultService
    {
        private readonly IMapper _mapper;
        private readonly IMessageProducer<AuditOut> _auditOutProducer;
        public CommonResultService(IMapper mapper, IProducerProvider auditOutProducer = null)
        {
            _mapper = mapper;
            _auditOutProducer = auditOutProducer.GetMessageProducer<AuditOut>();
        }
        public async Task ProduceAuditOutMessage(string message)
        {

            if (message != null)
            {
                AuditOut auditOut = new AuditOut();
                auditOut.Message = message;

                var messageWrapper = new Message<AuditOut>
                {
                    Payload = auditOut,
                    Key = Guid.NewGuid().ToString(),
                    MessageId = Guid.NewGuid().ToString(),
                };
                MessagePartitionInfo result = await _auditOutProducer.ProduceAsync(messageWrapper);
            }
        }
        public ComplexResultFlag GetMatchingResultFlag(ComplexResultFlag[] resultFlagRanges, Gender gender, string dob, int ageNbr, string ageType, DateTime calcDobFromDate = default, string performingFacility = "")
        {
            return SharedFunctions.GetMatchingResultFlag(resultFlagRanges, gender, dob, ageNbr, ageType, calcDobFromDate, performingFacility);
        }
        public ReportModel UpdateResultFlagRangeInReportModel(ReportModel mappedReport, Bioreference.LIS.Report report)
        {
            foreach (ReportAnalyte test in report.Analytes.List)
            {
                var mappedAnalyte = mappedReport.Analytes.List.Analyte
                    .FirstOrDefault(a => a.ID == test.ID);
                if (mappedAnalyte != null && mappedAnalyte.RefAnalyte != null)
                {
                    ComplexResultFlag flag = test.GetMatchingResultFlag();
                    mappedAnalyte.RefAnalyte.ResultFlagRange = _mapper.Map<ComplexResultFlagModel>(flag);
                    SetReferenceRange(mappedAnalyte.RefAnalyte, flag, test.RefAnalyte);
                }
            }
            foreach (ReportAnalytePanel panel in report.AnalytePanels.List)
            {
                foreach (ReportAnalyte test in panel.Analytes.List)
                {
                    var mappedAnalyte = mappedReport.AnalytePanels.List.AnalytePanel.
                        First(a => a.Id == panel.Id).Analytes.List.Analyte.FirstOrDefault(a => a.ID == test.ID);
                    if (mappedAnalyte != null && mappedAnalyte.RefAnalyte != null)
                    {
                        ComplexResultFlag flag = test.GetMatchingResultFlag();
                        mappedAnalyte.RefAnalyte.ResultFlagRange = _mapper.Map<ComplexResultFlagModel>(flag);
                        SetReferenceRange(mappedAnalyte.RefAnalyte, flag, test.RefAnalyte);


                    }
                }
            }
            return mappedReport;
        }
        private void SetReferenceRange(RefAnalyteModel refAnalyteModel, ComplexResultFlag flag , RefAnalyte refAnalyte)
        {
            if (!refAnalyte.AllowInternalRefRanges && (refAnalyte.ReferenceLabId != 0 || !string.IsNullOrEmpty(refAnalyte.ReferenceRange)))
            {
                refAnalyteModel.ReferenceRangeValue = refAnalyte.ReferenceRange;
                return;
            }
            refAnalyteModel.ReferenceRangeValue = flag != null ? flag.ReferenceRangeText : refAnalyte.ReferenceRange;
        }
        public void UpdateIsBlindInReport(Bioreference.LIS.Report report)
        {
            foreach (ReportAnalyte analyte in report.Analytes.List)
            {
                analyte.IsBlinded = report.IsBlindedTest(analyte);
            }
            foreach (ReportAnalytePanel panel in report.AnalytePanels.List)
            {
                foreach (ReportAnalyte analyte in panel.Analytes.List)
                {
                    analyte.IsBlinded = report.IsBlindedTest(analyte);
                }
            }
        }
    }
}
