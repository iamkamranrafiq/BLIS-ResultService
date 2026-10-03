using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.Contracts.Result
{
    public class Incident
    {
        public string SourceID { get; set; }
        public List<string> Markers { get; set; }
        public string Identifier { get; set; }
        public string Category { get; set; }
        public string IncidentType { get; set; }
        public string Notes { get; set; }
        public string AccessionNumber { get; set; }
        public string DateOfService { get; set; } 
        public string ClientID { get; set; }
        public List<TestInfo> Tests { get; set; }
    }

    public class TestInfo
    {
        public string TestCode { get; set; }
        public string Description { get; set; }
        public string Result { get; set; }
    }

}
