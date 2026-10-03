namespace Bioreference.ResultService.Application.Model
{
    public class AppSettingsInboundEngine
    {
        public const string SectionName = "Bioreference.InboundProcessor";
        public string? ResultOut_AllergensPanelLikeTests { get; set; }
        public string? StatusOut_ArchivePath { get; set; }
        public string? ResultOut_IGECode { get; set; }

        public bool Inbound_DynamicAddTests { get; set; }
        public bool Inbound_AutoReleaseResults { get; set; }
        public bool Inbound_ClearExistingData { get; set; }
        public bool BaseEngine_EnableDuplicateNTESuppress { get; set; }
        public bool Inbound_EnableDuplicateNTESuppress { get; set; }

        public string? ResultOut_ChannelId { get; set; }
        public string? ResultOut_FetchCount { get; set; }

        public bool ResultOut_SuppressNTEs { get; set; }
        public string? ResultOut_SuppressNTEsPath { get; set; }

        public string? MSH_SendingApp { get; set; }
        public string? MSH_SendingFacilityCode { get; set; }
        public string? MSH_ReceivingApp { get; set; }
        public string? MSH_ReceivingFacility { get; set; }
        public string? MSH_MessageType { get; set; }
        public string? MSH_MessageEvent { get; set; }
        public string? MSH_ProcessingId { get; set; }
        public string? MSH_AcceptAckType { get; set; }
        public string? MSH_VersionId { get; set; }

        public string? TestDescriptionTitle { get; set; }
        public string? TestDescriptionWidth { get; set; }
        public string? ResultTitle { get; set; }
        public string? ResultWidth { get; set; }
        public string? FlagTitle { get; set; }
        public string? FlagWidth { get; set; }
        public string? ReferenceRangeTitle { get; set; }
        public string? ReferenceRangeWidth { get; set; }
        public string? UnitsTitle { get; set; }
        public string? UnitsWidth { get; set; }

        public bool SupressTestCodes { get; set; }
        public List<string>? SupressTestCodes_AllowCodes { get; set; }
        public List<string>? SupressTestCodes_AcctNbrByPass { get; set; }

        public string? ReferencePrefix { get; set; }
        public bool InboundEngine_ErrorOnCorrected { get; set; }

        public string? TabularTextNTEPrefix { get; set; }
        public List<string>? Inbound_TNPEquivalent { get; set; }

        public string? OGAAccessionPrefix { get; set; }
        public string? OGAFilePrefix { get; set; }
        public string? BIOFilePrefix { get; set; }
        public string? BIOAccessionPrefix { get; set; }

        public string? OrderOut_StatusMsgPath { get; set; }
        public string? OrderOut_ArchiveMsgPath { get; set; }
        public string? OrderOut_StatusMsgPath2 { get; set; }

        public List<string>? BaseEngine_NtesLineBreakAscii { get; set; }
        public bool Inbound_ResultThrowErrorForWarning { get; set; }

        public string? ProcessDataType { get; set; }
        public bool ResultOut_SendOrderedCode { get; set; }

        public string? TotalChannelCount { get; set; }
        public string? CurrentChannelIndex { get; set; }

        public List<string>? ResultOut_SuppressNteAsBlockRefLabIds { get; set; }
        public List<string>? ResultOut_PanelCommonResult { get; set; }
        public List<string>? ResultOut_SendCommentsAccStartsWith { get; set; }

        public string? VmdConfig { get; set; }
        public List<string>? ResultsOut_TestsNoRefBypass { get; set; }

        public string? AllergenReportType { get; set; }
        public bool ResultOut_SendCommentCode { get; set; }
        public List<string>? ResultOut_BypassNTEForCommentCodes { get; set; }

        public bool OrderOut_SendPv1 { get; set; }
        public string? OrderOut_PatientLocation { get; set; }

        public bool InboundEngine_DisregardPanelCode { get; set; }
        public List<string>? Inbound_BypassResultValues { get; set; }
        public bool Inbound_LogWarnings { get; set; }
        public bool Inbound_SendAuditToAuditChannel { get; set; }

        public string? AuditFilePrefix { get; set; }
        public string? AuditChannelPath { get; set; }

        public List<string>? ResultOut_TestCodesToNotReport { get; set; }
        public List<string>? Inbound_FlagBlankResultsForStatus { get; set; }
        public List<string>? Inbound_TNPResultsForStatus { get; set; }

        public bool Inbound_DeleteDuplicateCmts { get; set; }
        public bool ToFollowEnabled { get; set; }

        public string? CalculationsTestName { get; set; }
        public string? OrderingCode { get; set; }
        public string? LabId { get; set; }
        public string? PID_PatientAccountNumber { get; set; }

        public List<string>? Inbound_BypassAnalyte { get; set; }
        public bool Inbound_SkipNTESegments { get; set; }
        public List<string>? SendAsSTResultType { get; set; }

        public List<string>? Outbound_StatusMessagePaths { get; set; }
        public string? B24K_Release_SleepTime { get; set; }
        public string? B24K_Calc_SleepTime { get; set; }

        public string? SendOutLabId { get; set; }
        public string ReferenceLabId { get; set; }

        public bool Inbound_ErrorOnCorrected { get; set; }  
        public List<string>? TNPCommentsToReport { get; set; }

        public string? TotalInstances { get; set; }
        public string? ModInstance { get; set; }
        public string? ChannelName { get; set; }
        public List<string>? Inbound_AccessionPrefixTNPOBR { get; set; }

        public int TestCodePadding { get; set; } 
    }
}
