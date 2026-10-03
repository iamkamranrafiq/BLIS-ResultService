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
            CreateMap<RuleDTO, WebAPI.Model.RuleDTO>();
            CreateMap<ViewRulesModel, WebAPI.Model.ViewRulesModel>();

            CreateMap<TNPSearchModel, WebAPI.Model.TNPSearchModel>();

        }
    }
}
