using AutoMapper;
using Bioreference.ResultService.Abstractions.Application.RapidResult;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Common.Common.RapidResult;
using Bioreference.ResultService.Common.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace Bioreference.ResultService.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RapidResultController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IRapidResultService _rapidResultService;
        private readonly ILogger<RapidResultController> _logger;
        public RapidResultController(ILogger<RapidResultController> logger, IRapidResultService rapidResultService, IMapper mapper)
        {
            _rapidResultService = rapidResultService;
            _mapper = mapper;
            _logger = logger;
        }
        [HttpGet]
        public async Task<IActionResult> RapidResults(int templateId, bool isCompleted = false, int pageNo = 0, int pageSize = 0)
        {
            ActivityHelper.SetLogKey($"RapidResult:List:TemplateId:{templateId};IsCompleted:{isCompleted};PageNo:{pageNo};PageSize:{pageSize}");

            var result = await _rapidResultService.RapidResults(templateId, isCompleted, pageNo, pageSize); 
            return Ok(result);
        }
        [HttpGet("Template")]
        public async Task<IActionResult> GetRapidResultTemplate(int templateId)
        {
            ActivityHelper.SetLogKey($"RapidResult:Template:TemplateId:{templateId}");

            var response = await _rapidResultService.RapidResultTemplateDetail(templateId);
            var mappedResponse = _mapper.Map<WebAPI.Model.RapidResultTemplateModel>(response);
            return Ok(mappedResponse);
        }

        [HttpGet("Accessions")]
        public async Task<IActionResult> RapidResultAccessions(int rapidResultId)
        {
            ActivityHelper.SetLogKey($"RapidResult:Accessions:RapidResultId:{rapidResultId}");

            var response = await _rapidResultService.RapidResultAccessions(rapidResultId);
            var mappedResponse = _mapper.Map<WebAPI.Model.RapidResultModel>(response);
            return Ok(mappedResponse);
        }
        [HttpPost("CheckAccession")]
        public async Task<IActionResult> CheckAddedAccession([FromBody] AccessionOrderModel requests)
        {

            if (!string.IsNullOrWhiteSpace(requests.AccessionNbr))
            {
                ActivityHelper.SetAccessionLogKey(requests.AccessionNbr.Trim());
                _logger.LogInformation(
                    "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                    requests.AccessionNbr.Trim(),
                    DateTime.UtcNow
                );
            }
            else
            {
                ActivityHelper.SetLogKey($"RapidResult:CheckAccession:TemplateId:{requests?.templateId};RapidResultId:{requests?.rapidResultId};IsControl:{requests?.IsControl}");
            }

            var response = await _rapidResultService.CheckAddedAccession(requests);
            return Ok(response);

        }

        [HttpDelete]
        public async Task<IActionResult> DeleteRapidResult(int rapidResultId)
        {
            ActivityHelper.SetLogKey($"RapidResult:Delete:RapidResultId:{rapidResultId}");

            var response = await _rapidResultService.DeleteRapidResult(rapidResultId);
            return Ok(response);
        }

        [HttpPost("Event")]
        public async Task<IActionResult> Event([FromBody] EventRapids events)
        {
            var eventCount = events?.Events?.Count ?? 0;
            ActivityHelper.SetLogKey($"RapidResult:Event:TemplateId:{events?.templateId};RapidResultId:{events?.rapidResultId};EventCount:{eventCount}");

            var response = await _rapidResultService.EventProcessor(events);
            var mappedResponse = _mapper.Map<WebAPI.Model.RapidResultModel>(response);
            return Ok(mappedResponse);

        }

        [HttpGet("Outstanding")]
        public async Task<IActionResult> OutstandingRapidResults(int templateId, int pastDays)
        {
            ActivityHelper.SetLogKey($"RapidResult:Outstanding:TemplateId:{templateId};PastDays:{pastDays}");

            var response = await _rapidResultService.OutstandingRapidResults(templateId, pastDays);
            return Ok(response);

        }
        [HttpGet("Templates")]
        public async Task<IActionResult> GetRapidResultTemplates()
        {
            ActivityHelper.SetLogKey("RapidResult:Templates");

            var response = await _rapidResultService.RapidResultTemplates();
            return Ok(response);
        }
        [HttpGet("Controls")]
        public async Task<IActionResult> GetRapidResultControls(int templateId)
        {
            ActivityHelper.SetLogKey($"RapidResult:Controls:TemplateId:{templateId}");

            var response = await _rapidResultService.RapidResultControl(templateId);
            return Ok(response);
        }

    }
}
