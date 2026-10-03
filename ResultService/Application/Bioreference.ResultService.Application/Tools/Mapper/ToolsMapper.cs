using AutoMapper;
using Bioreference.LIS;
using Bioreference.ResultService.Application.Model;
using Bioreference.RuleEngine;
using Bioreference.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bioreference.ResultService.Application.Tools.Mapper
{
    public class ToolsMapper :Profile
    {
        public ToolsMapper()
        {
            CreateMap<Rule, RuleDTO>()
               .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
               .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority));

            CreateMap<TNPPending, TNPSearchModel>();

        }
    }
    public class RuleSetProfile : Profile
    {
        public RuleSetProfile()
        {
            CreateMap<RuleSet, RuleSetDto>()
                .ForMember(dest => dest.RuleSetId, opt => opt.MapFrom(src => src.RuleSetId))
                .ForMember(dest => dest.RuleSetName, opt => opt.MapFrom(src => src.RuleSetName));
        }
    }
}
