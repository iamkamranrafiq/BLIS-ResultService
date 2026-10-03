using Bioreference.ResultService.Application.Model.Enum;

namespace Bioreference.ResultService.Application.Model
{
    public class OrderSearchModel
    {
        public int OrderId { get; set; }
        public int ReportId { get; set; }
        public resultStatusTypeModel ResultStatus { get; set; }
        public string AccessionNbr { get; set; }
        public long EUID { get; set; }
        public string PatientName { get; set; }
        public string AccountNumber { get; set; }
        public DateTime ServiceDate { get; set; }
        public DateTime CollectionDate { get; set; }
        public bool IsReportHold { get; set; }
        public string ClientID { get; set; }

    }
}