using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Model.Report.ClinicalTrials
{
    public class ClinicalTrialOrderInfo
    {
        public string AccessionNumber { get; set; }     
        public DateTime DateOfCollection { get; set; }
        public DateTime DateOfService { get; set; }
        public string AccountNo { get; set; }
        public string VisitNumber { get; set; }
        public string StudyNumber { get; set; }

    }
}
