using AutoMapper;
using Bioreference.LIS;
using Bioreference.ResultService.Application.Model;


namespace Bioreference.ResultService.Application.RapidResult.Mapper
{
    public class RapidResultProfile : Profile
    {
        public RapidResultProfile()
        {

            CreateMap<LIS.RapidResult, RapidResultModel>()
                .ForMember(dest => dest.List, opt => opt.MapFrom(src => src.List))
                .ForMember(dest => dest.Analytes, opt => opt.MapFrom(src => src.Analytes));

            CreateMap<LIS.RapidResultAnalyte, RapidResultAnalyteModel>()
                 //.ForMember(dest => dest.Analyte, opt => opt.MapFrom(src => src.Analyte))
                 .ForMember(dest => dest.ResultHistory, opt => opt.MapFrom(src => src.ResultHistory));

            CreateMap<LIS.ReportAnalyteHistory, ReportAnalyteHistoryModel>();

            CreateMap<RefAnalyte, RefAnalyteModel>()
              .ForMember(dest => dest.ResultFlagRanges, opt => opt.MapFrom(src => src.ResultFlagRanges))
              .ForMember(dest => dest.Calculation, opt => opt.MapFrom(src => src.Calculation))
              .ForMember(dest => dest.CommentsSelection, opt => opt.MapFrom(src => src.CommentsSelection));

            CreateMap<LIS.RapidResult.AnalyteInfo, AnalyteInfoModel>();

            CreateMap<LIS.RapidResultTemplate, RapidResultTemplateModel>()
                .ForMember(dest => dest.List, opt => opt.MapFrom(src => src.List))
                .ForMember(dest => dest.ControlList, opt => opt.MapFrom(src => src.ControlList));

            CreateMap<LIS.RapidResultTemplateControlList, RapidResultTemplateControlListModel>()
          .ForMember(dest => dest.List, opt => opt.MapFrom(src => src.Cast<RapidResultTemplateControl>().ToList()));


            CreateMap<LIS.RapidResultTemplateControl, RapidResultTemplateControlModel>();

            CreateMap<LIS.RapidResultTemplateAnalyte, RapidResultTemplateAnalyteModel>();

        }
    }
}
