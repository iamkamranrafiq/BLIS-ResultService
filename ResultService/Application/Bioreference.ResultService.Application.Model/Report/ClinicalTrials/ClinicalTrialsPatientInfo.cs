using Bioreference.ResultService.Application.Model.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Model.Report.ClinicalTrials
{
    public class ClinicalTrialsPatientInfo
    {
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public GenderModel Gender { get; set; }
        public string Age { get; set; }
        public long EUID { get; set; }
        public string DOB { get; set; }
        public int PatientID { get; set; }
    }
}
