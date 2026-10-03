using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.Contracts.Result
{
    public class GenecysResult
    {
        public string AccessionNumber { get; set; }
        public List<GenecysTestResult> Results { get; set; }
    }

    public class GenecysTestResult
    {
        public string TestCode { get; set; }
        public string TestName { get; set; }
        public string Result { get; set; }
        public string PerformingFacility { get; set; }
        public string InstrumentId { get; set; }
    }
}
