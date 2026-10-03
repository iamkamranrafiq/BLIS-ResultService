using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Model.Order
{
    public class OrderInfoModel
    {
        public string AccessionNumber { get; set; }
        public int OrderId { get; set; }
        public bool IsReportHold { get; set; }
        public DateTime DateOfService { get; set; }
        public string ClientID { get; set; }
    }
}
