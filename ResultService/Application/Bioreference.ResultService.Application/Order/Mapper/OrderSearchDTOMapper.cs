using Bioreference.LIS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using AutoMapper;
using Bioreference.ResultService.Application.Model;

namespace Bioreference.ResultService.Application.Order.Mapper
{
    public class OrderSearchProfile : Profile
    {
        public OrderSearchProfile()
        {
            CreateMap<ReportInfo, OrderSearchModel>();
            
            CreateMap<OrderSearchModel, ReportInfo>();
        }
    }
}
