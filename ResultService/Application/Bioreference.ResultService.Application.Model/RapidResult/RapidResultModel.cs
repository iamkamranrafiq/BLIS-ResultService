


namespace Bioreference.ResultService.Application.Model
{
    public class RapidResultModel
    {
        public int Id { get; set; }

        public object IdentifierId { get; set; }

        public RapidResultAnalyteModel[] List { get; set; }

        public AnalyteInfoModel[] Analytes { get; set; }

        public DateTime DateCreated { get; set; }

        public string CreatedBy { get; set; }

        public DateTime DateUpdated { get; set; }

        public string UpdatedBy { get; set; }

        public int RapidResultTemplateId { get; set; }

        public object IsCompleted { get; set; }

        public bool IsClosed { get; set; }

        public bool IsDuplicateAccessionValidationSuccessfull { get; set; }
    }

}
