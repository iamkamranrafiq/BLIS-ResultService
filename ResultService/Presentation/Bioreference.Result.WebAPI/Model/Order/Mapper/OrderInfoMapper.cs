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
    public class OrderInfoProfile : Profile
    {
        public OrderInfoProfile()
        {

            CreateMap<OrderModel, WebAPI.Model.OrderModel>();

            CreateMap<OrderSearchModel, WebAPI.Model.OrderSearchModel>();

            CreateMap<SpecimenModel, WebAPI.Model.SpecimenModel>();

            CreateMap<AOEAnswerModel, WebAPI.Model.AOEAnswerModel>();

            CreateMap<TestModel, WebAPI.Model.TestModel>();

        }

    }
}
