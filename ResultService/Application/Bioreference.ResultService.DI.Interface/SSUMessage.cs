namespace Bioreference.ResultService.DI.Interface
{
    public class SSUMessage
    {
        public List<MSH> MSH { get; set; }
        public EquipmentDetail EquipmentDetail { get; set; }
        public SpecimenContainerDetail SpecimenContainerDetail { get; set; }
        public List<OBX> Result { get; set; }
    }
}
