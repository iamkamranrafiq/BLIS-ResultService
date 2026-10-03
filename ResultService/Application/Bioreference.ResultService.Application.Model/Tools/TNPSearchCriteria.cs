using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Model
{
    public class TNPSearchCriteria
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string TestCodes { get; set; }
        public string Account { get; set; }
        public int PendlingListId { get; set; }
        public int PageNo { get; set; }
        public int PageSize { get; set; }
    }
}
