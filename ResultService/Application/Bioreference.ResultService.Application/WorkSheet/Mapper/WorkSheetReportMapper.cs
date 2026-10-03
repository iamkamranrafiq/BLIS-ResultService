using AutoMapper;
using Bioreference.LIS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bioreference.Common.TestMaster;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Application.Model.WorkSheet;

namespace Bioreference.ResultService.Application.Lookup.Mapper
{
    public class WorkSheetReportMapper : Profile
    {
        public WorkSheetReportMapper()
        {

            CreateMap<RackWorksheetReportItem, WorkSheetReportModel>();

            CreateMap<RackWorksheetTemplate, RackWorkSheetTemplateModel>()
                 .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                 .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                     .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
                 .ForMember(dest => dest.SpecimensPerRack, opt => opt.MapFrom(src => src.SpecimensPerRack));

            CreateMap<RackWorksheetReport, RackWorkSheetModel>()
                 .ForMember(dest => dest.WorkSheetId, opt => opt.MapFrom(src => src.RackWorksheetId))
                 .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => src.DateCreated))
                 .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
                 .ForMember(dest => dest.AssignedTo, opt => opt.MapFrom(src => src.AssignedUser))
                 .ForMember(dest => dest.Total, opt => opt.MapFrom(src => src.List.Count))
                 .ForMember(dest => dest.Released, opt => opt.MapFrom(src => src.GetTotalReleased()));
        }
    }
}
