namespace Bioreference.ResultService.DI.Interface
{
    public class SpecimenContainerDetail
    {
        public string ExternalAccessionIdentifier { get; set; } = string.Empty;
        public string AccessionIdentifier { get; set; } = string.Empty;
        public string ContainerIdentifier { get; set; } = string.Empty;
        public string PrimaryContainerIdentifier { get; set; } = string.Empty;
        public string EquipmentContainerIdentifier { get; set; } = string.Empty;
        public string SpecimenSource { get; set; } = string.Empty;
        public string RegistrationDateTime { get; set; } = string.Empty;
        public string ContainerStatus { get; set; } = string.Empty;
        
    }
}
