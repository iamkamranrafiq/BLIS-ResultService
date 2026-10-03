using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.WebAPI.Model
{
    public class COCBatchReportModel
    {
        public int CocBatchId { get; set; }
        public string CocBatchName { get; set; }
        public string CocStartAccession { get; set; }
        public string CocEndAccession { get; set; }
        public DateTime DateCreated { get; set; }
        public string CreatedBy { get; set; }
        public int Total { get; set; }
        public int Released { get; set; }
        public bool HasReleasedAccessions { get; set; }

    }
}
