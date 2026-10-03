using Bioreference.ResultService.WebAPI.Model;

namespace Bioreference.ResultService.WebAPI.Model
{
    public class ComplexResultFlagModel
    {
        public string DivisionCode { get; set; }
        public bool IsDefault { get; set; }
        public string Name { get; set; }
        public int Id { get; set; }
        public genderFlagTypeModel Gender { get; set; }
        public bool UseAgeQualifier { get; set; }
        public int AgeFrom { get; set; }
        public int AgeTo { get; set; }
        public string AgeType { get; set; }
        public string ReferenceRangeText { get; set; }
        public bool UseRanges { get; set; }
        public bool UseValues { get; set; }

        public ComplexRangesModel Ranges { get; set; }
        public ComplexValuesModel Values { get; set; }
    }
}
