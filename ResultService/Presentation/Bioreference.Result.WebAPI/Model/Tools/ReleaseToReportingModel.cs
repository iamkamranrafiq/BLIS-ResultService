using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.WebAPI.Model
{
    public class ReleaseToReportingModel
    {
        public int OrderID { get;set; }
        public bool IsReportHold { get;set; }
    }
}
