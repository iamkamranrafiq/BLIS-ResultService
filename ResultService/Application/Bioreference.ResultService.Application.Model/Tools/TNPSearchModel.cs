using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Model
{
    public class TNPSearchModel
    {
        public string AccessionNbr { get; set; }
        public DateTime Dos { get; set; }
        public string PanelCode { get; set; }
        public string TestCode { get; set; }
        public string ResultStatus { get; set; }
    }
}
