using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.WebAPI.Model
{
    public class AlertModel
    {
        public int Id { get; set; }

        public string Code { get; set; }

        public string Description { get; set; }

        public string Instruction { get; set; }

        public bool BlockAutoRelease { get; set; }

    }
}
