namespace Bioreference.ResultService.WebAPI.Model
{
    public class RapidResultTemplateModel
    {
        public int Id { get; set; }
        public bool PrintLandscape { get; set; }
        public bool RequiresVerification { get; set; }
        public bool PrintPathologistSheet { get; set; }
        public string Name { get; set; }
        public bool HideResult { get; set; }
        public bool ConfirmDeactivate { get; set; }
        public RapidResultTemplateAnalyteModel[] List { get; set; }
        public RapidResultTemplateControlListModel ControlList { get; set; }
    }
}
