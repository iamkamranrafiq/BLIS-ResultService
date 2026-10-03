using AutoMapper;
using Bioreference.Common.Lab;
using Bioreference.LIS;
using Bioreference.ResultService.Application.Model;
using DefaultCommentNamespace = Bioreference.ResultService.Application.Model.Report.RefAnalyte.DefaultComment;
using Bioreference.RuleEngine;

namespace Bioreference.ResultService.Application.Report.Mapper
{
    public class ReportProfile : Profile
    {
        public ReportProfile()
        {
            // Map Report to ReportDTO
            CreateMap<Bioreference.LIS.Report, ReportModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ID))
                .ForMember(dest => dest.AccessionNumber, opt => opt.MapFrom(src => src.AccessionNbr))
                .ForMember(dest => dest.AccountNumber, opt => opt.MapFrom(src => src.AccountNumber))
                .ForMember(dest => dest.AgeNumber, opt => opt.MapFrom(src => src.AgeNbr))
                .ForMember(dest => dest.AuditItems, opt => opt.MapFrom(src => src.GetFormattedAuditItems ?? new List<string>()))
                .ForMember(dest => dest.AccessionIdentifier, opt => opt.MapFrom(src => src.AccessionIdentifier))
                .ForMember(dest => dest.ParentIdentifierId, opt => opt.MapFrom(src => src.AccessionIdentifier))
                .ForMember(dest => dest.GetFormattedAuditItems, opt => opt.MapFrom(src => src.GetFormattedAuditItems ?? new List<string>()))
                .ForMember(dest => dest.Analytes, opt => opt.MapFrom(src => src.Analytes))
                .ForMember(dest => dest.AnalytePanels, opt => opt.MapFrom(src => src.AnalytePanels))
                .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.Comments))
                .ForMember(dest => dest.PriorResults, opt => opt.MapFrom(src => src.PriorResults));

            CreateMap<PriorResult, PriorResultModel>()
                .ForMember(dest => dest.ReportId, opt => opt.MapFrom(src => src.ReportId))
                .ForMember(dest => dest.ReportAnalyteId, opt => opt.MapFrom(src => src.ReportAnalyteId))
                .ForMember(dest => dest.AnalyteCode, opt => opt.MapFrom(src => src.AnalyteCode))
                .ForMember(dest => dest.EUID, opt => opt.MapFrom(src => src.EUID))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.ResultValue, opt => opt.MapFrom(src => src.ResultValue))
                .ForMember(dest => dest.ResultStatus, opt => opt.MapFrom(src => src.ResultStatus))
                .ForMember(dest => dest.ResultStatusStr, opt => opt.MapFrom(src => src.ResultStatusStr))
                .ForMember(dest => dest.OrderId, opt => opt.MapFrom(src => src.OrderId))
                .ForMember(dest => dest.AccessionNbr, opt => opt.MapFrom(src => src.AccessionNbr))
                .ForMember(dest => dest.DateServiced, opt => opt.MapFrom(src => src.DateServiced))
                .ForMember(dest => dest.ReleaseDate, opt => opt.MapFrom(src => src.ReleaseDate))
                .ForMember(dest => dest.ResultReleasedUser, opt => opt.MapFrom(src => src.ResultReleasedUser))
                .ForMember(dest => dest.PriorEuidResultValue, opt => opt.MapFrom(src => src.PriorEuidResultValue))
                .ForMember(dest => dest.DOB, opt => opt.MapFrom(src => src.DOB))
                .ForMember(dest => dest.DeltaRuleChange, opt => opt.MapFrom(src => src.DeltaDifference()))
                .ForMember(dest => dest.DeltaHoldRule, opt => opt.MapFrom(src => src.DeltaHoldRule));

            // Mapping the Analyte entities to the respective DTOs
            CreateMap<ReportAnalytes, ReportAnalytesModel>()
                .ForMember(dest => dest.List, opt => opt.MapFrom(src => src.List));

            CreateMap<ReportAnalyteList, ReportAnalyteListModel>()
                .ForMember(dest => dest.Analyte, opt => opt.MapFrom(src => src.Cast<ReportAnalyte>().ToList()));

            CreateMap<ReportAnalyte, ReportAnalyteModel>()
                .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.Comments))
                .ForMember(dest => dest.RefAnalyte, opt => opt.MapFrom(src => src.RefAnalyte));

            // Mapping the RefAnalyte to the respective DTOs
            CreateMap<RefAnalyte, RefAnalyteModel>()
                .ForMember(dest => dest.ResultFlagRanges, opt => opt.MapFrom(src => src.ResultFlagRanges))
                .ForMember(dest => dest.Calculation, opt => opt.MapFrom(src => src.Calculation))
                .ForMember(dest => dest.CommentsSelection, opt => opt.MapFrom(src => src.CommentsSelection));

            CreateMap<ComplexResultFlag, ComplexResultFlagModel>()
                .ForMember(dest => dest.Ranges, opt => opt.MapFrom(src => src.Ranges))
                .ForMember(dest => dest.Values, opt => opt.MapFrom(src => src.Values));

            CreateMap<Calculation, CalculationModel>();

            // Complex Value Mapper
            CreateMap<ComplexValues, ComplexValuesModel>()
                .ForMember(dest => dest.List, opt => opt.MapFrom(src => src.List))
                .ForMember(dest => dest.DropdownList, opt => opt.MapFrom(src => src.DropdownList));

            CreateMap<ComplexValueList, ComplexValueListModel>()
                .ForMember(dest => dest.ComplexValue, opt => opt.MapFrom(src => src.Cast<ComplexValue>().ToList()));

            CreateMap<ComplexValue, ComplexValueModel>();

            // Complex Range Mapper
            CreateMap<ComplexRanges, ComplexRangesModel>()
                .ForMember(dest => dest.List, opt => opt.MapFrom(src => src.List));

            CreateMap<ComplexRangeList, ComplexRangeListModel>()
                .ForMember(dest => dest.ComplexRange, opt => opt.MapFrom(src => src.Cast<ComplexRange>().ToList()));

            CreateMap<ComplexRange, ComplexRangeModel>();

            // CommentsSelection Mapping
            CreateMap<DefaultCommentList, DefaultCommentListModel>()
                .ForMember(dest => dest.List, opt => opt.MapFrom(src => src));

            CreateMap<CommentList, CommentListModel>()
                .ForMember(dest => dest.DefaultComment, opt => opt.MapFrom(src => src.Cast<Comment>().ToList()));

            CreateMap<Comment, DefaultCommentNamespace.CommentModel>();

            // Mapping the Panel entities to the respective DTOs
            CreateMap<ReportAnalytePanels, ReportAnalytePanelsModel>()
                .ForMember(dest => dest.List, opt => opt.MapFrom(src => src.List));

            CreateMap<ReportAnalytePanelList, ReportAnalytePanelListModel>()
                .ForMember(dest => dest.AnalytePanel, opt => opt.MapFrom(src => src.Cast<ReportAnalytePanel>().ToList()));

            CreateMap<ReportAnalytePanel, ReportAnalytePanelModel>()
                 .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.Comments))
                .ForMember(dest => dest.Panel, opt => opt.MapFrom(src => src.Panel)); 

            CreateMap<RefAnalytePanel, RefAnalytePanelModel>();
            // Mapping the Comment entities to the respective DTOs
            CreateMap<ReportComments, ReportCommentsModel>()
                .ForMember(dest => dest.List, opt => opt.MapFrom(src => src.List));

            CreateMap<ReportCommentList, ReportCommentListModel>()
                .ForMember(dest => dest.ReportComment, opt => opt.MapFrom(src => src.Cast<ReportComment>().ToList()));

            CreateMap<ReportComment, ReportCommentModel>();

            // Mapping the Attachment entities to the respective DTOs
            CreateMap<Attachments, AttachmentsModel>()
                .ForMember(dest => dest.List, opt => opt.MapFrom(src => src.List));

            CreateMap<AttachmentList, AttachmentListModel>()
                .ForMember(dest => dest.Attachment, opt => opt.MapFrom(src => src.Cast<Attachment>().ToList()));

            CreateMap<Attachment, AttachmentModel>();

            // Mapping the ReportAlert entities to the respective DTOs
            CreateMap<ReportAlerts, ReportAlertsModel>()
                .ForMember(dest => dest.List, opt => opt.MapFrom(src => src.List));

            CreateMap<ReportAlertList, ReportAlertListModel>()
                .ForMember(dest => dest.ReportAlert, opt => opt.MapFrom(src => src.Cast<ReportAlert>().ToList()));

            CreateMap<ReportAlert, ReportAlertModel>()
                .ForMember(dest => dest.Alert, opt => opt.MapFrom(src => src.Alert));

            CreateMap<Alert, AlertModel>();
          

            CreateMap<ReportInfo, ReportInfoSearchCriteria>();

            CreateMap<CriteriaDefinition, CriteriaDefinitionModel>();
            CreateMap<CorrectedReasons, CorrectedReasonsModel>();
            CreateMap<CorrectedReasonsModel, CorrectedReasons>()
            .ForSourceMember(src => src.DeptResponsible, opt => opt.DoNotValidate())
            .ForSourceMember(src => src.ReportingDept, opt => opt.DoNotValidate())
            .ForMember(dest => dest.DeptResponsible, opt => opt.Ignore())
            .ForMember(dest => dest.ReportingDepartment, opt => opt.Ignore());
        }
    }
}
