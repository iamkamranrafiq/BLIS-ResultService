using Bioreference.ResultService.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Abstractions.Application.Audit
{
    public interface IAuditService
    {
        public Task<List<AuditModel>> Search(AuditSearchCriteria request);
    }
}
