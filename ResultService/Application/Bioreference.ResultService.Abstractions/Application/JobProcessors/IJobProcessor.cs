
using Bioreference.ResultService.Application.Model.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Abstractions.Application.Processors
{
    public interface IJobProcessor
    {
        public void Process(ReflexMessage message);
    }
}
