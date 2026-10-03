using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.WebAPI.Model
{
    public class COCSearchCriteria
    {
        public string Day { get; set; }
        public string Month { get; set; }
        public string Year { get; set; }
        public string SequenceStart { get; set; }
        public string SequenceEnd { get; set; }
        public int BatchId { get; set; }

    }
}
