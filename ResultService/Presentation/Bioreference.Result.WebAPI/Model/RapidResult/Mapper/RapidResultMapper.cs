using AutoMapper;
using Bioreference.ResultService.Application.Model;

namespace Bioreference.ResultService.Application.RapidResult.Mapper
{
    public class RapidResultProfile : Profile
    {
        public RapidResultProfile()
        {
            CreateMap<RapidResultsModel, WebAPI.Model.RapidResultsModel>();
            CreateMap<AccessionOrderResultModel, WebAPI.Model.AccessionOrderResultModel>();
            CreateMap<AnalyteInfoModel, WebAPI.Model.RapidResultAnalytesModel>();

            CreateMap<RapidResultModel,WebAPI.Model.RapidResultModel>()
                .ForMember(dest => dest.List, opt => opt.MapFrom(src => src.List))
                .ForMember(dest => dest.Analytes, opt => opt.MapFrom(src => src.Analytes));

            CreateMap<RapidResultAnalyteModel, WebAPI.Model.RapidResultAnalyteModel>()
               //.ForMember(dest => dest.Analyte, opt => opt.MapFrom(src => src.Analyte))
               .ForMember(dest => dest.ResultHistory, opt => opt.MapFrom(src => src.ResultHistory));

            CreateMap<ReportAnalyteHistoryModel, WebAPI.Model.ReportAnalyteHistoryModel>();
            CreateMap<RefAnalyteModel, WebAPI.Model.RefAnalyteModel>();

            CreateMap<AnalyteInfoModel, WebAPI.Model.AnalyteInfoModel>();


            CreateMap<RapidResultTemplateModel, WebAPI.Model.RapidResultTemplateModel>()
    .ForMember(dest => dest.List, opt => opt.MapFrom(src => src.List))
    .ForMember(dest => dest.ControlList, opt => opt.MapFrom(src => src.ControlList));

            CreateMap<RapidResultTemplateControlListModel, WebAPI.Model.RapidResultTemplateControlListModel>()
          .ForMember(dest => dest.List, opt => opt.MapFrom(src => src.List));


            CreateMap<RapidResultTemplateControlModel, WebAPI.Model.RapidResultTemplateControlModel>();

            CreateMap<RapidResultTemplateAnalyteModel, WebAPI.Model.RapidResultTemplateAnalyteModel>();
        }
    }
}
