namespace Bioreference.ResultService.Application.Model.AppSettings.ReportOutNonCum
{
    public class ReportOutNonCumSettings
    {
        public List<string> ATPCommentsToReport { get; set; }
        public string BIOFilePrefix { get; set; }
        public int CurrentChannelIndex { get; set; }
        public string? FlagTitle { get; set; }
        public List<string> Lenetix_TestCodes { get; set; }
        public string? MSH_AcceptAckType { get; set; }
        public string? MSH_MessageEvent { get; set; }
        public string? MSH_MessageType { get; set; }
        public string? MSH_ProcessingId { get; set; }
        public string? MSH_ReceivingApp { get; set; }
        public string? MSH_ReceivingFacility { get; set; }
        public string? MSH_SendingApp { get; set; }
        public string? MSH_SendingFacilityCode { get; set; }
        public string? MSH_VersionId { get; set; }
        public List<string>? Outbound_StatusMessagePaths { get; set; }
        public int ProcessDataType { get; set; }
        public string? ReferenceRangeTitle { get; set; }
        public bool ResultOut_AccStatus_Enable { get; set; }
        public List<string>? ResultOut_AccStatus_ValidPrefixes { get; set; }
        public int ResultOut_ChannelId { get; set; }
        public int ResultOut_FetchCount { get; set; }
        public string? ResultOut_IGECode { get; set; }
        public List<string> ResultOut_SendCommentsAccStartsWith { get; set; }
        public bool ResultOut_SendOrderedCode { get; set; }
        public List<string> ResultOut_SuppressNteAsBlockRefLabIds { get; set; }
        public bool ResultOut_SuppressNTEs { get; set; }
        public string? ResultOut_SuppressNTEsPath { get; set; }
        public List<string> ResultOut_TestCodesToNotReport { get; set; }
        public List<string> ResultsOut_TestsNoRefBypass { get; set; }
        public string? ResultTitle { get; set; }
        public List<string> SendAsSTResultType { get; set; }
        public bool SPMTNP { get; set; }
        public bool StatisticsLogEnable { get; set; }
        public int StatisticsLogTimeout { get; set; }
        public string? TabularTextNTEPrefix { get; set; }
        public List<string> TestCodes { get; set; }
        public string? TestDescriptionTitle { get; set; }
        public List<string> TNPCommentsToReport { get; set; }
        public bool ToFollowEnabled { get; set; }
        public int TotalChannelCount { get; set; }
        public string? UnitsTitle { get; set; }
    }
}
