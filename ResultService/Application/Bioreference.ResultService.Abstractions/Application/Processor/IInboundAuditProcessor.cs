using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Abstractions.Application.Processor
{
    public interface IInboundAuditProcessor
    {
        public void ProcessMessage(string flatWire);
    }
}
