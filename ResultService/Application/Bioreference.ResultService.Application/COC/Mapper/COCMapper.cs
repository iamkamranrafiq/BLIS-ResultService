using AutoMapper;
using Bioreference.LIS;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Application.Model.COC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.COC.Mapper
{
    public class COCProfile : Profile
    {
        public COCProfile()
        {
            CreateMap<CocBatchReport, COCBatchReportModel>()
                .ForMember(dest => dest.CocBatchName, opt => opt.MapFrom(src => src.CocBatchName))
                .ForMember(dest => dest.CocBatchId, opt => opt.MapFrom(src => src.CocBatchId))
                .ForMember(dest => dest.CocStartAccession, opt => opt.MapFrom(src => src.CocStartAccession))
                .ForMember(dest => dest.CocEndAccession, opt => opt.MapFrom(src => src.CocEndAccession))
                .ForMember(dest => dest.DateCreated, opt => opt.MapFrom(src => src.DateCreated))
                .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
                .ForMember(dest => dest.Total, opt => opt.MapFrom(src => src.List.Count))
                .ForMember(dest => dest.HasReleasedAccessions, opt => opt.MapFrom(src => src.IsHasReleasedAccessions()))
                .ForMember(dest => dest.Released, opt => opt.MapFrom(src => src.GetTotalReleased()));

            CreateMap<CocBatchReportItem, COCBatchReportItemModel>()
                .ForMember(dest => dest.ReportId, opt => opt.MapFrom(src => src.ReportId))
                .ForMember(dest => dest.CocBatchId, opt => opt.MapFrom(src => src.CocBatchId))
                .ForMember(dest => dest.CocBatchAccessionId, opt => opt.MapFrom(src => src.CocBatchAccessionId))
                .ForMember(dest => dest.DateServiced, opt => opt.MapFrom(src => src.DateServiced))
                .ForMember(dest => dest.AlertCount, opt => opt.MapFrom(src => src.AlertCount))
                .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => src.PatientName))
                .ForMember(dest => dest.PendingCount, opt => opt.MapFrom(src => src.PendingCount))
                .ForMember(dest => dest.TransmitStatus, opt => opt.MapFrom(src => src.TransmitStatus))
                .ForMember(dest => dest.RreCount, opt => opt.MapFrom(src => src.RRECount))
                .ForMember(dest => dest.ResultStatus, opt => opt.MapFrom(src => src.ResultStatus));

            CreateMap<CocBatchAccession, COCBatchReportItemModel>();

            CreateMap<COCBatchAccessionModel, CocBatchAccession>()
                 .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => src.PatientName))
                .ForMember(dest => dest.AccessionNbr, opt => opt.MapFrom(src => src.AccessionNbr));

            CreateMap<CocBatchAccession, COCBatchAccessionModel>();

            CreateMap<CocBatch, COCModel>()
                .ForMember(dest => dest.BatchDay, opt => opt.MapFrom(src => src.batchDD))
                .ForMember(dest => dest.BatchMonth, opt => opt.MapFrom(src => src.batchMM))
                .ForMember(dest => dest.BatchYear, opt => opt.MapFrom(src => src.batchYYYY))
                .ForMember(dest => dest.BatchName, opt => opt.MapFrom(src => src.BatchName))
                .ForMember(dest => dest.BatchStartAccession, opt => opt.MapFrom(src => src.BatchStartingAccession))
                .ForMember(dest => dest.BatchEndAccession, opt => opt.MapFrom(src => src.BatchEndingAccession))
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id));


            CreateMap<CocBatchAccessionEditResponse, CocBatchAccessionEditStatusModel>()
               .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.CocBatchEditResponseMessage))
               .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.CocBatchAccessionEditStatus));

        }
    }
}
