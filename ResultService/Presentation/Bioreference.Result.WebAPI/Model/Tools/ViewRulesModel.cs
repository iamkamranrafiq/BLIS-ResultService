using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.WebAPI.Model
{
    public class ViewRulesModel
    {
        public DateTime LastUpdated { get; set; }
        public List<RuleDTO> Rules { get; set; }
    }

    public class RuleDTO
    {
        public int Priority { get; set; }
        public string Name { get; set; }
    }
}
