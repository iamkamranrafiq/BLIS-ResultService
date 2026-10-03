

namespace Bioreference.ResultService.WebAPI.Model
{
    public class WorkSheetReportModel
    {
        public string RackId { get; set; }
        public int RackWorksheetId { get; set; }
        public string AccessionNbr { get; set; }
        public int Sequence { get; set; }
        public int RackPosition { get; set; }
        public resultStatusTypeModel ResultStatus { get; set; }
        public transmitStatusTypeModel TransmitStatus { get; set; }
        public int PendingCount { get; set; }
        public int AlertCount { get; set; }
        public int ReportId { get; set; }
        public int RackWorksheetTemplateId { get; set; }
        public int SpecimenId { get; set; }
        public bool Has4kTest { get; set; }
    }
}
