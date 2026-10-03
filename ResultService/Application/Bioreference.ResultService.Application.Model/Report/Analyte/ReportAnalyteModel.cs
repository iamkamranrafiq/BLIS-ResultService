using Bioreference.ResultService.Application.Model.Enum;


namespace Bioreference.ResultService.Application.Model
{
    public class ReportAnalyteModel
    {
        public RefAnalyteModel RefAnalyte { get; set; }
        public string PriorReleasedValue { get; set; }
        public string ReleasedValue { get; set; }
        public int ReleasedStatus { get; set; }
        public DateTime PriorReleaseDate { get; set; }
        public string PerformingFaciltiy { get; set; }
        public string AccessioningFacility { get; set; }
        public int RefLabPerformingFacilityId { get; set; }
        public string CorrectedResultReason { get; set; }
        public string PreviousFlagValue { get; set; }
        public int RackWorksheetId { get; set; }
        public string CurrentFlagValue { get; set; }
        public string SPMOrderTestId { get; set; }
        public string RapidResultsType { get; set; }
        public int RapidResultId { get; set; }
        public DateTime ReleaseDate { get; set; }
        // public DataClassBase Parent { get; set; }
        public long ID { get; set; }
        public string Code { get; set; }
        public string AnalyteName { get; set; }
        public criticalTypeModel CriticalType { get; set; }
        public string[] OrderingAnalyteCodes { get; set; }
        public string OrderingCodes { get; set; }
        public string InstrumentId { get; set; }
        public string AnalyticalUnitName { get; set; }
        public bool IsRuleResultStatusSet { get; set; }
        public string AnalyticalUnitSerial { get; set; }
        public int InstrumentDivisionId { get; set; }
        public string AnalyzerName { get; set; }
        public string AnalyzerModel { get; set; }
        public string AnalyzerVendor { get; set; }
        public string Instrument { get; set; }
        public string InstrumentAlt1 { get; set; }
        public string InstrumentAlt2 { get; set; }
        public string InstrumentAlt3 { get; set; }
        public string FlagValue { get; set; }
        public string PreviousResultValue { get; set; }
        public string ResultValue { get; set; }
        public bool AuditDoubleEntry { get; set; }
        public bool IsNewAnalyteAdded { get; set; }
        public string Range { get; set; }
        public bool RangeUpdated { get; set; }
        public bool UnitsUpdated { get; set; }
        public bool FlagUpdated { get; set; }
        public bool StatusUpdated { get; set; }
        public bool IsResultManual { get; set; }
        public List<string> GetFormattedAuditItems { get; set; }
        public bool IsCOCReviewed { get; set; }
        public string COCApprover { get; set; }
        public string CocApproverFirstName { get; set; }
        public string CocApproverLastName { get; set; }
        public string CocApproverMiddleName { get; set; }
        public string CocApproverEmpNbr { get; set; }
        public DateTime CocApprovedDate { get; set; }
        public bool IsFlagDeleted { get; set; }
        public bool ToFollowSent { get; set; }
        public int CodeTypeId { get; set; }
        public string CalculatibleResultValue { get; set; }
        public bool IsPreliminaryReleased { get; set; }
        public bool ReportingHold { get; set; }
        public bool IsCopiedAnalyte { get; set; }
        public bool IsTestHold { get; set; }
        public bool IsDeltaHold { get; set; }
        public resultStatusTypeModel ResultStatusBeforeRuleRun { get; set; }
        public bool IsPOC { get; set; }
        public bool IsDoubleEntry { get; set; }
        public reReleaseStatusTypeModel ReReleaseStatus { get; set; }
        public int SampleStatus { get; set; }
        public DateTime SampleStatusUpdateDate { get; set; }
        public DateTime InstrumentLoadTime { get; set; }
        public bool ReleaseFromUI { get; set; }
        public SPMStatusValueModel SPMStatus { get; set; }
        public DateTime ResultDate { get; set; }
        public ReportCommentsModel Comments { get; set; }
        public AttachmentsModel AnalyteAttachments { get; set; }
        public ReportAlertsModel Alerts { get; set; }
        //   public Analyte Analyte { get; set; }
        public resultStatusTypeModel ResultStatus { get; set; }
        public transmitStatusTypeModel TransmitStatus { get; set; }
        public string SpecimenRackId { get; set; }
        public string SpecimenRackPosition { get; set; }
        public string SpecimenRackSequence { get; set; }
        public string SpecimenAlt1 { get; set; }
        public string SpecimenAlt2 { get; set; }
        public string StudyNumber { get; set; }
        public string VisitNumber { get; set; }
        public string PriorEUIDResultValue { get; set; }
        public bool IsPresumptiveHold { get; set; }
        public string SpecimenCodes { get; set; }
        public string DeltaHoldRule { get; set; }
        public string ParentTestCode { get; set; }
        public bool ManualReReleaseFlag { get; set; }
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public bool CanRelease { get; set; }
        public bool IsBlinded { get; set; }
        public string ResultReleasedUser { get; set; }
        public string ResultAnalyzedTechUser { get; set; }

    }
}
