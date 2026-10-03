using Bioreference.ResultService.Common.Enumerations;
namespace Bioreference.ResultService.Common.Common.RapidResult
{
    public class EventRapidData
    {
        public string AccessionNbr { get; set; } = string.Empty;
        public bool IsControl { get; set; } = false;
        public bool ChkPrelimOnly { get; set; } = false;
        public long ReportAnalyteId { get; set; } = 0;
        public int RowNumber { get; set; } = 0;
        public string AnalyteCode { get; set; } = string.Empty;
        public string ResultValue { get; set; } = string.Empty;
        public bool ClearResults { get; set; } = false; 

    }


}
