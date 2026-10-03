using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Abstractions.Application.Jobs
{
    public interface IBLISJob
    {
        public Task<bool> Execute();
    }
}
