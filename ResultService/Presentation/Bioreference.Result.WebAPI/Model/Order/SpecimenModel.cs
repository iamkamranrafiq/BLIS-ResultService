using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.WebAPI.Model
{
    public class SpecimenModel
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public int Qty { get; set; }
        public string Temp { get; set; }
        public string Flag { get; set; }
    }
}
