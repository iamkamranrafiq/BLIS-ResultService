using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.WebAPI.Model
{
    public class AuditModel
    {
        public string PropertyName { get; set; }
        public string AuditType { get; set; }
        public string FromValue { get; set; }
        public string ToValue { get; set; }
        public string UserName { get; set; }
        public string TestCode { get; set; }
        public string TestName { get; set; }
        public DateTime EventDate { get; set; }
    }
}
