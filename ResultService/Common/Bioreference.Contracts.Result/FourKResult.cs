using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.Contracts.Result
{
    public class FourKResult
    {
        public string AccessionNumber { get; set; }
        public string SerialNumber { get; set; }
        public string Test { get; set; }
        public string Result { get; set; }
        public string UserName { get; set; }
    }
}
