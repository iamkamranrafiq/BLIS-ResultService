using Bioreference.ResultService.Application.Model.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bioreference.ResultService.Common.Helpers;

namespace Bioreference.ResultService.Application.Model
{
    public class ComplexResultFlagModel
    {
        private string _referenceRangeText;

        public string DivisionCode { get; set; }
        public bool IsDefault { get; set; }
        public string Name { get; set; }
        public int Id { get; set; }
        public genderFlagTypeModel Gender { get; set; }
        public bool UseAgeQualifier { get; set; }
        public int AgeFrom { get; set; }
        public int AgeTo { get; set; }
        public string AgeType { get; set; }
        
        /// <summary>
        /// Reference range text with automatic line ending normalization.
        /// Ensures Windows-style line endings (\r\n) are preserved even after XML parsing.
        /// </summary>
        public string ReferenceRangeText 
        { 
            get => _referenceRangeText;
            set => _referenceRangeText = value.NormalizeToWindows();
        }
        public bool UseRanges { get; set; }
        public bool UseValues { get; set; }

        public ComplexRangesModel Ranges { get; set; }
        public ComplexValuesModel Values { get; set; }
    }
}
