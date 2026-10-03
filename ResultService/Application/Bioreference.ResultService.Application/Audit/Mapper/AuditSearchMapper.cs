using AutoMapper;
using Bioreference.Data.Audit;
using Bioreference.LIS;
using Bioreference.ResultService.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Audit.Mapper
{
    public class AuditSearchMapper : Profile
    {
        public AuditSearchMapper()
        {
            CreateMap<ReportAuditManager.AuditInfo, AuditModel>();


        }
    }
}
