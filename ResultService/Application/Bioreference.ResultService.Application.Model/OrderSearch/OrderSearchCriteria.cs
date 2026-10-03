using Bioreference.ResultService.Application.Model.Enum;
namespace Bioreference.ResultService.Application.Model
{
    public class OrderSearchCriteria
    {
        public string AccessionNumber { get; set; }
        public string AccountNumber { get; set; }
        public string PatientName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public transmitStatusTypeModel TransmitStatus { get; set; }
        public List<string> UserDivisionCodes { get; set; }
        public long EUID { get; set; } = -1;
    }
}
