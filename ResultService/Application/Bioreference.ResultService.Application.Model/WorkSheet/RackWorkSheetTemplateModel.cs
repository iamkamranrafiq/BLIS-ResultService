using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Model.WorkSheet
{
    public class RackWorkSheetTemplateModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int SpecimensPerRack { get; set; }
        public string Type { get; set; }
    }
}
