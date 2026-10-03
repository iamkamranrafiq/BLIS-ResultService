using AutoMapper;
using Bioreference.Data.Audit;
using Bioreference.LIS;
using Bioreference.ResultService.Abstractions.Application.Audit;
using Bioreference.ResultService.Abstractions.Application.RapidResult;
using Bioreference.ResultService.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Audit
{
    public class AuditService :IAuditService
    {
        private readonly IMapper _mapper;
        private readonly IRapidResultService _rapidResultService;

        public AuditService(IMapper mapper, IRapidResultService rapidResultService)
        {
            _mapper = mapper;
            _rapidResultService = rapidResultService;
        }

        public async Task<List<AuditModel>> Search(AuditSearchCriteria request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
             
            List<AuditModel> auditSearchResponse = new List<AuditModel>();
            ReportAuditManager reportAuditManager = new ReportAuditManager();
            var rapidResultTemplates = await _rapidResultService.RapidResultTemplates();
            var rapidResultTemplatesDic = rapidResultTemplates
                .List.ToDictionary(keySelector: m => m.Id, elementSelector: m => m.Name);

            ReportAuditManager.AuditInfo[] auditInfos = await Task.Run(()=> 
            reportAuditManager.Fetch(request.AccessionNumber, false, request.ServiceDate, rapidResultTemplatesDic, request.ReportId));
            
            if (auditInfos!=null)
            {
                auditSearchResponse = _mapper.Map<List<AuditModel>>(auditInfos);
            }
            return auditSearchResponse;
        }
    }
}
