using System;

namespace Bioreference.ResultService.WebAPI.Model
{
    public class OrderModel
    {
        public string AccessionNumber { get; set; }
        public string AccessionSys { get; set; }
        public DateTime DateOfService { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string AccountNumber { get; set; }
        public string AccountName { get; set; }
        public string OrderType { get; set; }
        public string OrderingProvider { get; set; }
        public DateTime DateCollected { get; set; }
        public string TimeCollected { get; set; }
        public string PayerType { get; set; }
        public string Physician { get; set; }
        public string EUID { get; set; }
        public string Comments { get; set; }
        public string SpecimenMsgs { get; set; }
        public string Priority { get; set; }
        public List<SpecimenModel> Specimens { get; set; }
        public List<TestModel> Tests { get; set; }
        public List<AOEAnswerModel> AOEAnswers { get; set; }
        public int PatientId { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string FirstName { get; set; }
        public string Gender { get; set; }
        public string Age { get; set; }
        public IsFastingTypeModel Fasting { get; set; }
        public string DOB { get; set; }
        public string StudyNumber { get; set; }

        public string VisitNumber { get; set; }
        public bool IsReportHold { get; set; }
        public int DivisionId { get; set; }


    }

}
