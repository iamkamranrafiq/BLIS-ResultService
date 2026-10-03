using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace Bioreference.LIS
{
    public class AccessionNbr
    {

        private string _accessionNbr;

        public AccessionNbr(string accessionNbr)
        {
            _accessionNbr = accessionNbr.Trim();
            // If _accessionNbr.Length <> 7 AndAlso _accessionNbr.Length <> 9 AndAlso Not IsNumeric(accessionNbr) Then
            // Throw New Exception("Accession Number must be numeric and either 7 or 9 digits in length.")
            // End If
        }

        public string Full
        {
            get
            {
                if (IsSevenDigits)
                {
                    return $"10{_accessionNbr}";
                }
                else
                {
                    return _accessionNbr;
                }
            }
        }

        public bool IsNJ1
        {
            get
            {
                return IsSevenDigits || IsNineDigits && _accessionNbr.StartsWith("10");
            }
        }

        public bool IsValid
        {
            get
            {
                return _accessionNbr.IsInteger() && (IsSevenDigits || IsNineDigits);
            }
        }

        public bool IsSevenDigits
        {
            get
            {
                return _accessionNbr.Length == 7;
            }
        }

        public string ToSevenDigits()
        {
            return Conversions.ToString(Interaction.IIf(IsNJ1 && IsNineDigits, _accessionNbr.Remove(0, 2), _accessionNbr));
        }

        public bool IsNineDigits
        {
            get
            {
                return _accessionNbr.Length == 9;
            }
        }

        public bool IsClinicalTrial
        {
            get
            {
                return IsValid && IsNineDigits && _accessionNbr.StartsWith("66");
            }
        }

        public override string ToString()
        {
            return _accessionNbr;
        }

    }
}