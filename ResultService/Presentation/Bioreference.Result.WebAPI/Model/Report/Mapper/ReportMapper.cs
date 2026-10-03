using AutoMapper;
using DefaultWebAPICommentNamespace = Bioreference.ResultService.WebAPI.Model.Report.RefAnalyte.DefaultComment;
using DefaultApplicationCommentNamespace = Bioreference.ResultService.Application.Model.Report.RefAnalyte.DefaultComment;

namespace Bioreference.ResultService.WebAPI.Report.Mapper
{
    public class ReportDTOProfile : Profile
    {
        public ReportDTOProfile()
        {
            // Map Report to ReportDTO
            CreateMap<Application.Model.ReportModel, WebAPI.Model.ReportModel>();

            CreateMap<Application.Model.PriorResultModel, WebAPI.Model.PriorResultModel>();

            // Mapping the Analyte entities to the respective DTOs
            CreateMap<Application.Model.ReportAnalytesModel, WebAPI.Model.ReportAnalytesModel>()
                .ForMember(dest => dest.List, opt => opt.MapFrom(src => src.List));

            CreateMap<Application.Model.ReportAnalyteListModel, WebAPI.Model.ReportAnalyteListModel>()
                .ForMember(dest => dest.Analyte, opt => opt.MapFrom(src => src.Analyte));

            CreateMap<Application.Model.ReportAnalyteModel, WebAPI.Model.ReportAnalyteModel>()
                .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.Comments))
                .ForMember(dest => dest.RefAnalyte, opt => opt.MapFrom(src => src.RefAnalyte));

            // Mapping the RefAnalyte to the respective DTOs
            CreateMap<Application.Model.RefAnalyteModel, WebAPI.Model.RefAnalyteModel>()
              //  .ForMember(dest => dest.ResultFlagRanges, opt => opt.MapFrom(src => src.ResultFlagRanges)) // Removing List of ResultFlagRanges
                .ForMember(dest => dest.ResultFlagRange, opt => opt.MapFrom(src => src.ResultFlagRange))
                .ForMember(dest => dest.Calculation, opt => opt.MapFrom(src => src.Calculation))
                .ForMember(dest => dest.CommentsSelection, opt => opt.MapFrom(src => src.CommentsSelection));

            CreateMap<Application.Model.ComplexResultFlagModel, WebAPI.Model.ComplexResultFlagModel>()
                .ForMember(dest => dest.Ranges, opt => opt.MapFrom(src => src.Ranges))
                .ForMember(dest => dest.Values, opt => opt.MapFrom(src => src.Values));

            CreateMap<Application.Model.CalculationModel, WebAPI.Model.CalculationModel>();

            // Complex Value Mapper
            CreateMap<Application.Model.ComplexValuesModel, WebAPI.Model.ComplexValuesModel>()
                .ForMember(dest => dest.List, opt => opt.MapFrom(src => src.List))
                .ForMember(dest => dest.DropdownList, opt => opt.MapFrom(src => src.DropdownList));

            CreateMap<Application.Model.ComplexValueListModel, WebAPI.Model.ComplexValueListModel>()
                .ForMember(dest => dest.ComplexValue, opt => opt.MapFrom(src => src.ComplexValue));

            CreateMap<Application.Model.ComplexValueModel, WebAPI.Model.ComplexValueModel>();

            // Complex Range Mapper
            CreateMap<Application.Model.ComplexRangesModel, WebAPI.Model.ComplexRangesModel>()
                .ForMember(dest => dest.List, opt => opt.MapFrom(src => src.List));

            CreateMap<Application.Model.ComplexRangeListModel, WebAPI.Model.ComplexRangeListModel>()
                .ForMember(dest => dest.ComplexRange, opt => opt.MapFrom(src => src.ComplexRange));

            CreateMap<Application.Model.ComplexRangeModel, WebAPI.Model.ComplexRangeModel>();

            // CommentsSelection Mapping
            CreateMap<Application.Model.DefaultCommentListModel, WebAPI.Model.DefaultCommentListModel>()
                .ForMember(dest => dest.List, opt => opt.MapFrom(src => src.List));

            CreateMap<Application.Model.CommentListModel, WebAPI.Model.CommentListModel>()
                .ForMember(dest => dest.DefaultComment, opt => opt.MapFrom(src => src.DefaultComment));

            CreateMap<DefaultApplicationCommentNamespace.CommentModel, DefaultWebAPICommentNamespace.CommentModel >();

            // Mapping the Panel entities to the respective DTOs
            CreateMap<Application.Model.ReportAnalytePanelsModel, WebAPI.Model.ReportAnalytePanelsModel>()
                .ForMember(dest => dest.List, opt => opt.MapFrom(src => src.List));

            CreateMap<Application.Model.ReportAnalytePanelListModel, WebAPI.Model.ReportAnalytePanelListModel>()
                .ForMember(dest => dest.AnalytePanel, opt => opt.MapFrom(src => src.AnalytePanel));

            CreateMap<Application.Model.ReportAnalytePanelModel, WebAPI.Model.ReportAnalytePanelModel>()
                 .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.Comments))
                .ForMember(dest => dest.Panel, opt => opt.MapFrom(src => src.Panel)); 

            CreateMap<Application.Model.RefAnalytePanelModel, WebAPI.Model.RefAnalytePanelModel>();
            // Mapping the Comment entities to the respective DTOs
            CreateMap<Application.Model.ReportCommentsModel, WebAPI.Model.ReportCommentsModel>()
                .ForMember(dest => dest.List, opt => opt.MapFrom(src => src.List));

            CreateMap<Application.Model.ReportCommentListModel, WebAPI.Model.ReportCommentListModel>()
                .ForMember(dest => dest.ReportComment, opt => opt.MapFrom(src => src.ReportComment));

            CreateMap<Application.Model.ReportCommentModel, WebAPI.Model.ReportCommentModel>();

            // Mapping the Attachment entities to the respective DTOs
            CreateMap<Application.Model.AttachmentsModel, WebAPI.Model.AttachmentsModel>()
                .ForMember(dest => dest.List, opt => opt.MapFrom(src => src.List));

            CreateMap<Application.Model.AttachmentListModel, WebAPI.Model.AttachmentListModel>()
                .ForMember(dest => dest.Attachment, opt => opt.MapFrom(src => src.Attachment));

            CreateMap<Application.Model.AttachmentModel, WebAPI.Model.AttachmentModel>();

            // Mapping the ReportAlert entities to the respective DTOs
            CreateMap<Application.Model.ReportAlertsModel, WebAPI.Model.ReportAlertsModel>()
                .ForMember(dest => dest.List, opt => opt.MapFrom(src => src.List));

            CreateMap<Application.Model.ReportAlertListModel, WebAPI.Model.ReportAlertListModel>()
                .ForMember(dest => dest.ReportAlert, opt => opt.MapFrom(src => src.ReportAlert));

            CreateMap<Application.Model.ReportAlertModel, WebAPI.Model.ReportAlertModel>()
                                .ForMember(dest => dest.Alert, opt => opt.MapFrom(src => src.Alert));
     
            CreateMap<Application.Model.AlertModel, WebAPI.Model.AlertModel>();

            CreateMap<Application.Model.ReportInfoSearchCriteria, WebAPI.Model.ReportInfoSearchCriteria>();

            CreateMap<Application.Model.CriteriaDefinitionModel, WebAPI.Model.CriteriaDefinitionModel>();
            CreateMap<Application.Model.CorrectedReasonsModel, WebAPI.Model.CorrectedReasonsModel>();
            CreateMap<Application.Model.CorrectedReasonsModel, WebAPI.Model.CorrectedReasonsModel>();
        }
    }
}
