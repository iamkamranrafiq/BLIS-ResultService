namespace Bioreference.ResultService.Application.Model
{
    public class AccessionOrderModel
    {
        public int? templateId { get; set; }
        public int? rapidResultId { get; set; }
        public string AccessionNbr { get; set; }
        public bool IsControl { get; set; }
    }
}
