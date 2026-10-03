using Bioreference.ResultService.Application.Model.Enum;

namespace Bioreference.ResultService.Application.Model.COC
{
    public class COCBatchReportItemModel
    {
        public int CocBatchId { get; set; }
        public int CocBatchAccessionId { get; set; }
        public int ReportId { get; set; }
        public string AccessionNbr { get; set; }
        public DateTime DateServiced { get; set; }
        public string PatientName { get; set; }
        public resultStatusTypeModel ResultStatus { get; set; }
        public transmitStatusTypeModel TransmitStatus { get; set; }
        public int PendingCount { get; set; }
        public int AlertCount { get; set; }
        public int RreCount { get; set; }
    }
}
