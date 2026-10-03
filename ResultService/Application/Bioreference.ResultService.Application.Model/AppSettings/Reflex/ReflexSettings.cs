using Org.BouncyCastle.Bcpg.OpenPgp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Model.AppSettings.Reflex
{
    public class ReflexSettings
    {
        public string ETSUrl { get; set; }
        public string BlisETSUrl { get; set; }
        public string ETSUserID { get; set; }
        public string ETSPassword { get; set; }
        public string IGECode { get; set; }
        public List<string> SuppressIGEReflexCodes { get; set; } = new List<string>();
        public List<string> SuppressTNPReflexCodes { get; set; } = new List<string>();
    }
}
