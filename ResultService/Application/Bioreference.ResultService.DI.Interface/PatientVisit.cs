namespace Bioreference.ResultService.DI.Interface
{
    public class PatientVisit
    {
        public string Set_ID { get; set; } = string.Empty;

        public string PatientClass { get; set; } = string.Empty;

        public string PatientLocation { get; set; }

        public PatientVisit(string strPatientClass = "", string strPatientLocation = "")
        {
            if (!string.IsNullOrEmpty(strPatientClass))
                PatientClass = strPatientClass;
            if (!string.IsNullOrEmpty(strPatientLocation))
                PatientLocation = strPatientLocation;
        }
    }
}
