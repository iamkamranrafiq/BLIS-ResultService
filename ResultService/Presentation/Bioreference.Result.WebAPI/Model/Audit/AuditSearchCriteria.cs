using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.WebAPI.Model
{
    public class AuditSearchCriteria
    {
        public int ReportId { get; set; }
        public DateTime ServiceDate { get; set; }
        public string AccessionNumber { get; set; }
    }
}
