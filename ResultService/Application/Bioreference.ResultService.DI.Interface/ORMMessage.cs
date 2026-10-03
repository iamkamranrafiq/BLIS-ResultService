namespace Bioreference.ResultService.DI.Interface
{
    public class ORMMessage
    {
        public MSH MSH { get; set; }
        public PatientInfo PatientInfo { get; set; }
        public List<PatientComment> PatientInfoComments { get; set; }
        public PatientVisit PatientVisit { get; set; }
        public List<LabAccession> LabAccession { get; set; }
        public List<LabComment> LabAccessionComment { get; set; }
        public List<LabReport> LabReports { get; set; }
        public List<Order> Order { get; set; }
        public List<LabComment> OrderComment { get; set; }
        public List<GrpORMOrder> GrpORMOrder { set; get; }
    }
}
