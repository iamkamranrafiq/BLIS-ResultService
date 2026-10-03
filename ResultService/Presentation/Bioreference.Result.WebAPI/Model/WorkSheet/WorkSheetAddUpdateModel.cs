using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.WebAPI.Model
{
    public class WorkSheetAddUpdateModel
    {
        public int Id { get; set; } 
        public int RackWorkSheetTemplateId { get; set; }
        public List<WorkSheetSpecimenModel> WorkSheetSpecimen { get; set; }
    }

    
}
