using AutoMapper;
using Bioreference.ResultService.Abstractions.Application.Lookup;
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
    public class ToolsController : Controller
    {
        private readonly IToolsService _toolsService;
        private readonly IMapper _mapper;

        public ToolsController(IToolsService toolsService, IMapper mapper)
        {
            _toolsService = toolsService;
            _mapper = mapper;
        }


        [HttpPost("Search")]
        public async Task<IActionResult> TNPSearch([FromBody] TNPSearchCriteria tNPSearchRequest)
        {
            ActivityHelper.SetLogKey($"Tools:TNPSearch:PendingListId:{tNPSearchRequest?.PendlingListId};PageNo:{tNPSearchRequest?.PageNo};PageSize:{tNPSearchRequest?.PageSize};Account:{tNPSearchRequest?.Account}");
            var result = await _toolsService.TNPSearch(tNPSearchRequest);
            var response = _mapper.Map<List<ResultService.WebAPI.Model.TNPSearchModel>>(result);
            return Ok(response);

        }

        [HttpPost("ReleaseToReporting")]
        public async Task<IActionResult> ReleaseToReporting([FromBody] List<ReleaseToReportingModel> releaseToReportingModel)
        {
            ActivityHelper.SetLogKey($"Tools:ReleaseToReporting:Count:{releaseToReportingModel?.Count ?? 0}");
            var response = await _toolsService.ReleaseToReporting(releaseToReportingModel);
            return Ok(new { result = response });

        }

        [HttpPost("BulkTNPRelease")]
        public async Task<IActionResult> BulkTNPRelease([FromBody] List<BulkTNPReleaseModel> bulkTNPReleaseModel)
        {
            ActivityHelper.SetLogKey($"Tools:BulkTNPRelease:Count:{bulkTNPReleaseModel?.Count ?? 0}");
            var response = await _toolsService.ReleaseBulkTNP(bulkTNPReleaseModel);
            return Ok(new { result = response });
        }
    }
}
