using Bioreference.ResultService.WebAPI.Model;

namespace Bioreference.ResultService.WebAPI.Model
{
    public class AnalyteModel
    {
        //public bool AttachCommentToParent { get; set; }
        public reportingTypeModel ReportingType { get; set; }
        public bool IsAgencyReportable { get; set; }
        //public int OutboundChannelId { get; set; }
        public CalculationModel Calculation { get; set; }
        public string FlagValue { get; set; }
        public string Category { get; set; }
        public string Code { get; set; }

        public DefaultCommentListModel CommentsSelection { get; set; }
        //public Critical[] Criticals { get; set; }
        public string Name { get; set; }
        public string ReferenceRange { get; set; }
        public string ReferenceRangeValue { get; set; }
        public resultTypeModel ResultType { get; set; }
        public List<ResultModel> ResultValues { get; set; }
        //public List<ComplexResultFlagModel> ResultFlagRanges { get; set; }  // Removing List of ResultFlagRanges
        public ComplexResultFlagModel ResultFlagRange { get; set; }
        public string Units { get; set; }
        public bool IsSendOut => ReferenceLabId != 0;
        public int ReferenceLabId { get; set; }
        public string ReferenceLabeAnalyteCode { get; set; }
        public int Sequence { get; set; }
        public bool IsRequired { get; set; }
        public bool IsReportable { get; set; }
        public bool IsCalculation => !string.IsNullOrEmpty(Calculation?.Expression?.Trim());
        //public TestCodeMappingList TestCodeAliasList { get; set; }
        public string RelatedBioTestCodes { get; set; }
        public string Analyzer { get; set; }
        public string AnalyzerChannelNumber { get; set; }
        public string FluidType { get; set; }
        public downloadTypeModel DownloadType { get; set; }
        public string TypeOfAnalyzerResult { get; set; }
        public string AutomaticVerificationRange { get; set; }
        public string AcceptableInstFlag { get; set; }
        public string Notes { get; set; }
        public string ReportableInfo { get; set; }
        public string AnalyzerInstrumentId { get; set; }
        public string LisInstrumentId { get; set; }
    }
}
