namespace Bioreference.ResultService.DI.Interface
{
    public class PatientInfo
    {
        public string ClientMRN { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;
        public string SocSecNum { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;        

        public string DOB { get; set; } = string.Empty;

        public string Age { get; set; } = string.Empty;

        public string Sex { get; set; } = string.Empty;

        public string Address1 { get; set; } = string.Empty;

        public string Address2 { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public string Zip { get; set; } = string.Empty;

        public string HomePhone { get; set; } = string.Empty;

        public string WorkPhone { get; set; } = string.Empty;

        public string SSN { get; set; } = string.Empty;

        public string AltField1 { get; set; } = string.Empty;

        public string AltField2 { get; set; } = string.Empty;

        public string AltField3 { get; set; } = string.Empty;

        public string Set_ID { get; set; } = string.Empty;

        public string PatientIdentifierList { get; set; } = string.Empty;

        public string DoctorReference { get; set; } = string.Empty;

        public string UpdateTrackingId { get; set; } = string.Empty;
    }
}
