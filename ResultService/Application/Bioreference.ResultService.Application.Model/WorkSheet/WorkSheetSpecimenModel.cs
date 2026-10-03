using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Model.WorkSheet
{
    public class WorkSheetSpecimenModel
    {
        public string RackId { get; set; }
        public int RackPosition { get; set; }
        public string AccessionNo { get; set; }
        public bool IsMarkForDelete { get; set; }
        public int ID { get; set; }

    }
}
