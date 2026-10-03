using System;
using System.Collections.Generic;
using System.Text;

namespace Bioreference.ScanningService.Application.Common.Interfaces
{
    public interface IBLISJob
    {
        public Task<bool> Execute();
    }
}
