using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.Contracts.Result
{
    public class GenecysOrder
    {
        public string AccessionNumber { get; set; }
        public string AccountNumber { get; set; }
        public string PatientName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string PatientSex { get; set; }
        public DateTime CollectionDate { get; set; }
        public DateTime DateServiced { get; set; }
        public string AccessionFacilityId { get; set; }
        public string SpecimenIdFull { get; set; }
        public List<GenecysOrderTest> TestCodes { get; set; }
    }
    public class GenecysOrderTest
    {
        public string PsycheTestCode { get; set; }
        public string B2TestCode { get; set; }
    }
}
