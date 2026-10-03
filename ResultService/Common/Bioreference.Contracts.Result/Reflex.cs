using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.Contracts.Result
{
    public class Reflex
    {
        public string OriginalAccessionNumber { get; set; }
        public string AccessionNumber { get; set; }
        public string OrderedCode { get; set; }
        public string ReflexCode { get; set; }
        public string DateOrdered { get; set; }
        public string ActionType { get; set; }
    }
}
