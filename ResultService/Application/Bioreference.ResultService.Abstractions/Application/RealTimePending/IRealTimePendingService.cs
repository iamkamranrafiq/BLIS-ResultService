using Bioreference.LIS;
using Bioreference.ResultService.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Abstractions.Application.RealTimePending
{
    public interface IRealTimePendingService
    {
        public Task<Pendings> RealTimePendingFetch(RTPSearchCriteria searchCriteriaDTO);
    }
}
