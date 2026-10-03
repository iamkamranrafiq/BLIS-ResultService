using AutoMapper;
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

            CreateMap<WorkSheetReportModel, WebAPI.Model.WorkSheetReportModel>();

            CreateMap<RackWorkSheetModel, WebAPI.Model.RackWorkSheetModel>();

            CreateMap<RackWorkSheetTemplateModel, WebAPI.Model.RackWorkSheetTemplateModel>();
        }
    }
}
