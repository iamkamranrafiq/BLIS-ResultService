using Bioreference.ResultService.Application.Model.Enum;

namespace Bioreference.ResultService.Application.Model
{
    public class ReportInfoSearchCriteria
    {
        public int OrderId { get; set; }
        public int ReportId { get; set; }
        public DateTime OrderDate { get; set; }
        public OrderPriorityModel Priority { get; set; }
        public int PendingCount { get; set; }
        public resultStatusTypeModel ResultStatus { get; set; }
        public int FlagCount { get; set; }
        public string PatientName { get; set; }
        public string AccountNumber { get; set; }
        public bool HasPreviousResultValue { get; set; }
        public string AccessionNbr { get; set; }
        public long EUID { get; set; }
        public transmitStatusTypeModel TransmitStatus { get; set; }
        public bool IsClinicalTrial { get; set; }
        public string DivisionList { get; set; }
    }
}
