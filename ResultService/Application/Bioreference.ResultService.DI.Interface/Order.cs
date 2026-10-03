using static Bioreference.LIS.OrderManager;

namespace Bioreference.ResultService.DI.Interface
{
    public class Order
    {
        public string ClientOrderNumber { get; set; } = string.Empty;

        public string AccessionNumber { get; set; } = string.Empty;

        public string ReferenceLabNumber { get; set; } = string.Empty;

        public string ClientAccountNumber { get; set; } = string.Empty;

        public string OrderingDoctorLast { get; set; } = string.Empty;

        public string OrderingDoctorFirst { get; set; } = string.Empty;

        public string OrderingDoctorMiddle { get; set; } = string.Empty;

        public string OrderingDoctorName { get; set; } = string.Empty;

        public string Priority { get; set; } = string.Empty;

        public string OrderDate { get; set; } = string.Empty;

        public string CollectDate { get; set; } = string.Empty;

        public string ReceivedDate { get; set; } = string.Empty;

        public string AltField1 { get; set; } = string.Empty;

        public string AltField2 { get; set; } = string.Empty;

        public string AltField3 { get; set; } = string.Empty;

        public string OrderControlId { get; set; } = string.Empty;

        public string OrderStatus { get; set; } = string.Empty;

        public string HasComments { get; set; } = string.Empty;

        public string ApproverFirstName { get; set; } = string.Empty;

        public string ApproverMiddleName { get; set; } = string.Empty;

        public string ApproverLastName { get; set; } = string.Empty;

        public string ApproverEmpNumber { get; set; } = string.Empty;

        public string ApprovedDate { get; set; } = string.Empty;

        public string ConfidentialityCode { get; set; } = string.Empty;

        public string TestCode { get; set; } = string.Empty;
        public List<LabComment> Comment { get; set; } = new List<LabComment>();  
        public string OrderStatusModifier { get; set; } = string.Empty;
    }
}
