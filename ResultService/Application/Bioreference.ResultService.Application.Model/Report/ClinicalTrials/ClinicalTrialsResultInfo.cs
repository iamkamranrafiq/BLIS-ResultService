using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Model.Report.ClinicalTrials
{
    public class ClinicalTrialsResultInfo
    {
        public string TestName { get; set; }
        public string TestCode { get; set; }
        public string ResultUnit { get; set; }  
        public string Result { get; set; }
        public string RangeLow { get; set; }
        public string RangeHigh { get; set; }
        public string Department { get; set; }  
        public string Flag { get; set; }    
        public List<string> Comments { get; set; }
        public bool Fast { get; set; }
        public string Category { get; set; }
        public string OrderingTestCode { get; set; }
        public List<string> Loincs { get; set; }
    }
}
