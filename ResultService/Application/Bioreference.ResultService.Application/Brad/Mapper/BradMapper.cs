using AutoMapper;
using Bioreference.LIS;
using Bioreference.ResultService.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Brad.Mapper
{
    public class BradProfile : Profile
    {
        public BradProfile()
        {
            CreateMap<SpecimenRequest, SpecimenRequestModel>();

            CreateMap<SpecimenRequestModel, SpecimenRequest>();
        }
    }
}
