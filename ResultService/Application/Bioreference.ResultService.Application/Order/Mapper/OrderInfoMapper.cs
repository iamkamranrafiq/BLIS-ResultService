using Bioreference.LIS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using AutoMapper;
using Bioreference.ResultService.Application.Model;
using System.Diagnostics.CodeAnalysis;

namespace Bioreference.ResultService.Application.Order.Mapper
{
    [ExcludeFromCodeCoverage]
    public class OrderInfoProfile : Profile
    {
        public OrderInfoProfile()
        {
            CreateMap<LIS.Order, OrderModel>()
                 .ForMember(dest => dest.AccountNumber, opt => opt.MapFrom(src => src.AccountNumber))
                 .ForMember(dest => dest.AccessionNumber, opt => opt.MapFrom(src => src.AccessionNbr))
                 .ForMember(dest => dest.AccessionSys, opt => opt.MapFrom(src => src.AccessionIdentifier))
                 .ForMember(dest => dest.DateOfService, opt => opt.MapFrom(src => src.DateOfService))
                 .ForMember(dest => dest.TimeCollected, opt => opt.MapFrom(src => src.TimeOfCollection))
                 .ForMember(dest => dest.DateCollected, opt => opt.MapFrom(src => src.DateOfCollection))
                 .ForMember(dest => dest.EUID, opt => opt.MapFrom(src => src.EUID))
                 .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.Comments))
                 .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority))
                 .ForMember(dest => dest.StudyNumber, opt => opt.MapFrom(src => src.StudyNumber))
                 .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.Patient.LastName))
                 .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.Patient.FirstName))
                 .ForMember(dest => dest.PatientId, opt => opt.MapFrom(src => src.Patient.ID))
                 .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Patient.Gender))
                 .ForMember(dest => dest.MiddleName, opt => opt.MapFrom(src => src.Patient.MiddleName))
                 .ForMember(dest => dest.DOB, opt => opt.MapFrom(src => src.Patient.DateOfBirth))
                 .ForMember(dest => dest.Fasting, opt => opt.MapFrom(src => src.Patient.IsFasting))
                 .ForMember(dest => dest.Physician, opt => opt.MapFrom(src => src.PrimaryPhysician.FullName))
                 .ForMember(dest => dest.VisitNumber, opt => opt.MapFrom(src => src.VisitNumber))
                 .ForMember(dest => dest.IsReportHold, opt => opt.MapFrom(src => src.IsReportHold))
                 .ForMember(dest => dest.DivisionId, opt => opt.MapFrom(src => src.DivisionId))
                 .ForMember(dest => dest.Specimens, opt => opt.Ignore())
                 .ForMember(dest => dest.Tests, opt => opt.Ignore())
                 .ForMember(dest => dest.AOEAnswers, opt => opt.Ignore());

            CreateMap<SpecimenModel, OrderSpecimen>()
             .ForMember(dest => dest.SpecimenCode, opt => opt.MapFrom(src => src.Code))
              .ForMember(dest => dest.SpecimenName, opt => opt.MapFrom(src => src.Description));

            CreateMap<OrderSpecimen, SpecimenModel>()
               .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.SpecimenCode))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.SpecimenName));

            CreateMap<AOEAnswerModel, OrderAnswer>()
              .ForMember(dest => dest.Question, opt => opt.MapFrom(src => src.Question))
               .ForMember(dest => dest.Answer, opt => opt.MapFrom(src => src.Answer));

            CreateMap<OrderAnswer, AOEAnswerModel>()
            .ForMember(dest => dest.Question, opt => opt.MapFrom(src => src.Question))
             .ForMember(dest => dest.Answer, opt => opt.MapFrom(src => src.Answer));

            CreateMap<TestModel, OrderTest>()
             .ForMember(dest => dest.OrderedTestName, opt => opt.MapFrom(src => src.TestName))
              .ForMember(dest => dest.OrderedTestCode, opt => opt.MapFrom(src => src.TestCode));

            CreateMap<OrderTest, TestModel>()
           .ForMember(dest => dest.TestName, opt => opt.MapFrom(src => src.OrderedTestName))
            .ForMember(dest => dest.TestCode, opt => opt.MapFrom(src => src.OrderedTestCode));

        }   
          
    }
}
