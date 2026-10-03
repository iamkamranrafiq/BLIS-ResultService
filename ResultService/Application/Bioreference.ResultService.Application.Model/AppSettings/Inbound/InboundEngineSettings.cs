namespace Bioreference.ResultService.Application.Model
{
    public class InboundEngineSettings
    {
        public bool InboundEngine_ProcessTNPInOBR { get; set; }
        public bool IsTNPFromComment { get; set; }
        public bool PrelimReleaseFlag { get; set; }
        public List<string> PrelimReleaseTestCodes { get; set; }
        public bool ProcessReferenceFacility { get; set; }
        public List<string> TestCodeListForPrelimRelease { get; set; }
        public List<string> TestCodesForAutoRelease { get; set; }
        public bool Inbound_AutoGen_PanelCmtInDummyOBX { get; set; }
        public int InstrumentQueryExpirationDays { get; set; }
        public bool SearchResultAccessionWithDOS { get; set; }

        //public string B2ToCHM_MessageThroughChannel { get; set; }
        //public string DiffDateOfService { get; set; }
        //public string FlagMapping { get; set; }
        //public string Inbound_ClearExistingData { get; set; }
        //public string Inbound_Max_RefRangeLength { get; set; }
        //public string Inbound_Max_ResultLength { get; set; }
        //public string Inbound_Max_UnitLength { get; set; }
        //public string Inbound_TreponemaCode { get; set; }
        //public string Inbound_Validate_RefRangeLength { get; set; }
        //public string Inbound_Validate_ResultLength { get; set; }
        //public string Inbound_Validate_TreponemaTotalAbs { get; set; }
        //public string Inbound_Validate_UnitLength { get; set; }
        //public string TNPEquivalent { get; set; }
    }
}
