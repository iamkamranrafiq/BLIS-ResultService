using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Model.Report.PriorResult
{
    public class PriorResultRequestModel
    {
        public int ReportId { get; set; }
        public string AnalyteCode { get; set; }
        public bool IncludeTNPQNS { get; set; } = true;
        public bool IncludeAllStatus { get; set; } = true;
    }
}
