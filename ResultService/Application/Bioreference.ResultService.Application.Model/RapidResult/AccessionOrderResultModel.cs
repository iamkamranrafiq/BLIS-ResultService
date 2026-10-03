namespace Bioreference.ResultService.Application.Model
{
    public class AccessionOrderResultModel
    {
        public string? AccessionNbr { get; set; }
        public bool IsControl { get; set; }
        public int StatusId { get; set; }             
        public string Status { get; set; }
        public bool Has4kTest { get; set; } = false;
    }
}
