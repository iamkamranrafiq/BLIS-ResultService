using AutoMapper;
using Bioreference.LIS;
using Bioreference.ResultService.Application.Model;


namespace Bioreference.ResultService.Application.Order.Mapper
{
    public class OrderCommentMapper : Profile
    {
        public OrderCommentMapper()
        {
            CreateMap<OrderComment, OrderCommentModel>()
            .ForMember(dest => dest.ExtApplicationType, opt => opt.MapFrom(src => src.ExtApplicationType))
            .ForMember(dest => dest.IsAutoAdded, opt => opt.MapFrom(src => src.IsAutoAdded))
            .ForMember(dest => dest.Text, opt => opt.MapFrom(src => src.Text))
            .ForMember(dest => dest.DateCreated, opt => opt.MapFrom(src => src.DateCreated))
            .ForMember(dest => dest.ID, opt => opt.MapFrom(src => src.ID))
            .ForMember(dest => dest.ExternalCommentCode, opt => opt.MapFrom(src => src.ExternalCommentCode));
        }
    }
    
}
