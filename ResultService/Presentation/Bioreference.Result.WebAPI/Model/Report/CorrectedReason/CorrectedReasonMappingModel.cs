namespace Bioreference.ResultService.WebAPI.Model
{
    public class CorrectedReasonMappingModel
    {
        public int CorrectedReasonMappingId { get; set; }
        public int SecondaryId { get; set; }
        public string SecondaryReasonName { get; set; }
        public int PhaseId { get; set; }
        public string PhaseName { get; set; }
        public int PrimaryId { get; set; }
        public string PrimaryReasonName { get; set; }
        public int CausedById { get; set; }
        public string CausedByName { get; set; }
        public int ControllableId { get; set; }
        public string ControllableName { get; set; }
    }
}
