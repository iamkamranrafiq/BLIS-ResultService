using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.WebAPI.Model
{
    public class CalculationModel
    {
        public string Expression { get; set; }
        public List<string> TestCodeList { get; set; }

    }
}
