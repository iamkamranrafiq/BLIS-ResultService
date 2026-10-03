
namespace Bioreference.ResultService.Application.Model
{
    public class CriteriaDefinitionModel
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }
        public string ClassName { get; set; }
        public string PropertyName { get; set; }
        public string CriteriaCode { get; set; }
        public string FullClassName { get; set; }
        public string[] UnitList { get; set; }
        public string[] ValueList { get; set; }
        public  List<CriteriaParameterModel> ParameterList { get; set; }
        public bool AllowRange { get; set; }
        public bool EvaluateAsBoolean { get; set; }
        public List<CriteriaValueModel> CriteriaValues { get; set; }

    }
}
