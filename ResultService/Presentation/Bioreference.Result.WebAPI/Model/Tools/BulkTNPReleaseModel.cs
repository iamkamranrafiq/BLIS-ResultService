using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.WebAPI.Model
{
    public class BulkTNPReleaseModel
    {
        public string AccessionNumber { get; set; }
        public string TestCode { get; set; }
        public string PanelCode { get; set; }
        public string CommentType { get; set; }
        public string Comment { get; set; }

    }
}
