
namespace Bioreference.ResultService.WebAPI.Model
{
    public class TestDetailModel : AnalyteModel
    {
        public long Id { get; set; }
        public bool AutoRelease { get; set; }
        public int Precision { get; set; }
        public string ReferenceLabOrderingAnalyteCode { get; set; }
        //public string AltInboundTestCode { get; set; }
        //public string AltOutboundTestCode { get; set; }
        //public string AltOutboundDescription { get; set; }
        //public string PreviousTestRangeValue { get; set; }
        //public string PreviousUnitValue { get; set; }
        //public string PreviousFlagValue { get; set; }
        //public string OriginalTestRangeValue { get; set; }
        //public string OriginalUnitValue { get; set; }
        //public string OriginalFlagValue { get; set; }
        //public bool RangeUpdated { get; set; }
        //public bool UnitsUpdated { get; set; }
        //public bool FlagUpdated { get; set; }
        //public bool IsFlagDeleted { get; set; }
        public bool AllowPreliminaryRelease { get; set; }
        public bool AllowInternalRefRanges { get; set; }
        public bool IsPOC { get; set; }
        public bool IsDoubleEntry { get; set; }
        //public List<TestInstrument> TestInstruments { get; set; }
        public bool IsInterfaced { get; set; }
        public string DepartmentShortname { get; set; }
    }
}
