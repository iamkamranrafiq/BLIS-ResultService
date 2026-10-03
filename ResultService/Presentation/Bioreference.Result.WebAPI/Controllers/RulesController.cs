using AutoMapper;
using Bioreference.ResultService.Abstractions.Application.Lookup;
using Bioreference.ResultService.Abstractions.Application.Report;
using Bioreference.ResultService.Abstractions.Application.Tools;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Application.Model.Tools;
using Bioreference.ResultService.Common.Helpers;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Bioreference.ResultService.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RulesController : Controller
    {
        private readonly IToolsService _toolsService;
        private readonly IReportService _reportService;
        private readonly IMapper _mapper;
        public RulesController(IToolsService toolsService, IReportService reportService, IMapper mapper)
        {
            _toolsService = toolsService;
            _reportService = reportService;
            _mapper = mapper;
        }


        [HttpGet("CriteriaDefinition")]
        public async Task<IActionResult> GetCriteriaDefinition(string className)
        {
            ActivityHelper.SetLogKey($"Rules:CriteriaDefinition:ClassName:{className}");
            var response = await _reportService.FetchCriteria(className);

            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            return Ok(JsonConvert.SerializeObject(response, settings));
        }

        [HttpGet("Rules")]
        public async Task<IActionResult> GetRules(int ruleSetId, int pageNo, int pageSize)
        {
            ActivityHelper.SetLogKey($"Rules:GetRules:RuleSetId:{ruleSetId};PageNo:{pageNo};PageSize:{pageSize}");
            var result = await _toolsService.GetRules(ruleSetId, pageNo, pageSize);
            var response = _mapper.Map<ResultService.WebAPI.Model.ViewRulesModel>(result);
            return Ok(response);

        }

        [HttpGet("RuleSet")]
        public async Task<IActionResult> GetRuleSet()
        {
            var result = await _toolsService.GetRuleSet();
            return Ok(result);

        }
    }
}
