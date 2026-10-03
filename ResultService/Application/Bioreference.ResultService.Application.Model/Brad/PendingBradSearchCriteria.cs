using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Model
{
    public class PendingBradSearchCriteria
    {
        public string AccessionNbr { get; set; }
        private string Date { get; set; }
        private string Name { get; set; }
        private string Acct { get; set; }
    }
}
