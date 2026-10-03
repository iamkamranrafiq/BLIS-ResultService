using AutoMapper;


namespace Bioreference.ResultService.WebAPI.Audit.Mapper
{
    public class AuditSearchMapper : Profile
    {
        public AuditSearchMapper()
        {
            CreateMap<Application.Model.AuditModel, WebAPI.Model.AuditModel>();
            CreateMap<Application.Model.AuditSearchCriteria, WebAPI.Model.AuditSearchCriteria>();

            CreateMap<WebAPI.Model.AuditModel, Application.Model.AuditModel>();
            CreateMap<WebAPI.Model.AuditSearchCriteria, Application.Model.AuditSearchCriteria>();
        }
    }
}
