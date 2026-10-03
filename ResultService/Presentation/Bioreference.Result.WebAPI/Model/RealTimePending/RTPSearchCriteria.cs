using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.WebAPI.Model
{
    public class RTPSearchCriteria
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int LastHours { get; set; }
        public int ExcludeHours { get; set; }
        public int PendingListId { get; set; }
        public bool RdoEnableDate { get; set; }
    }
}
