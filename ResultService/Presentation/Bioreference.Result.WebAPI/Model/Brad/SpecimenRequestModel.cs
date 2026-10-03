using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.WebAPI.Model
{
    public class SpecimenRequestModel
    {

        public string Comments { get; set; }

        public bool IsMasterRequired { get; set; }

        public int FridgeId { get; set; }

        public int PriorityTypeID { get; set; }

        public int RequestDeptID { get; set; }

        public string RequestedBy { get; set; }

        public string SpecimenNumbers { get; set; }

        public int SpecimenTypeID { get; set; }

        public int Quantity { get; set; }

    }
}
