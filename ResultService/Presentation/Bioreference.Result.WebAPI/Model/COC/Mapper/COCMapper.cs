using AutoMapper;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Application.Model.COC;
using Bioreference.ResultService.WebAPI.Model;
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
            CreateMap<Model.COC.COCBatchReportModel, WebAPI.Model.COCBatchReportModel>();
            CreateMap<Model.COC.COCBatchReportItemModel, WebAPI.Model.COCBatchReportItemModel>();
            CreateMap<Model.COC.COCBatchAccessionModel, WebAPI.Model.COCBatchAccessionModel>();
            CreateMap<Model.COC.COCModel, WebAPI.Model.COCModel>();
            CreateMap<Model.COC.COCAddEditResponseModel, WebAPI.Model.COCAddEditResponseModel>();
            CreateMap<Model.COC.CocBatchAccessionEditStatusModel, WebAPI.Model.CocBatchAccessionEditStatusModel>();

        }
    }
}
