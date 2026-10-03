using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Model.Report.ClinicalTrials
{
    public class ClinicalTrialsResponseModel
    {
        public ClinicalTrialOrderInfo ClinicalTrialOrderInfo { get; set; }
        public ClinicalTrialsPatientInfo ClinicalTrialPatientInfo { get; set; }
        public List<ClinicalTrialsResultInfo> ClinicalTrialsResultInfos { get; set; }
    }
}
