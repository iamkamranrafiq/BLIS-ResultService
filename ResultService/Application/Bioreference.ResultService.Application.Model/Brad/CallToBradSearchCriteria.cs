using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Model
{
    public class CallToBradSearchCriteria
    {
        public List<SpecimenRequestModel> walkInList { get; set; }
        public List<SpecimenRequestModel> nonWalkInList { get; set; }
    }
}
