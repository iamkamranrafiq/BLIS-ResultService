using AutoMapper;
using Bioreference.LIS;
using Bioreference.ResultService.Application.Model;

namespace Bioreference.ResultService.Application.Report.Mapper
{
    public class CorrectedReasonMappingProfile : Profile
    {
        public CorrectedReasonMappingProfile()
        {
            CreateMap <CorrectedReasonMapping, CorrectedReasonMappingModel>();
            CreateMap<CorrectedReasonMappings, CorrectedReasonMappingsModel>()
                  .ForMember(dest => dest.List, opt => opt.MapFrom(src => src.List));
        }
    
    }
}
