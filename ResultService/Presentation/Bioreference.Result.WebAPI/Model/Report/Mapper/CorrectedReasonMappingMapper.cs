using AutoMapper;


namespace Bioreference.ResultService.Application.Report.Mapper
{
    public class CorrectedReasonMappingProfile : Profile
    {
        public CorrectedReasonMappingProfile()
        {
            CreateMap <Application.Model.CorrectedReasonMappingModel, WebAPI.Model.CorrectedReasonMappingModel>();
            CreateMap<Application.Model.CorrectedReasonMappingsModel, WebAPI.Model.CorrectedReasonMappingsModel>()
                  .ForMember(dest => dest.List, opt => opt.MapFrom(src => src.List));
        }
    
    }
}
