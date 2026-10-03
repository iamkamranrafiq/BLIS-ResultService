using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.WebAPI.Model
{
    public class RackWorkSheetModel
    {
        public int WorkSheetId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public int Total { get; set; }
        public int Released { get; set; }
        public string AssignedTo { get; set; }
        public int Status { get; set; }
    }
}
