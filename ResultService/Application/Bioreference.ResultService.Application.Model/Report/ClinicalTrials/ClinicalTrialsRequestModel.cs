using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Model.Report.ClinicalTrials
{
    public class ClinicalTrialsRequestModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<string> ClientID { get; set; }
        public List<string> TestCode { get; set; }
    }
}
