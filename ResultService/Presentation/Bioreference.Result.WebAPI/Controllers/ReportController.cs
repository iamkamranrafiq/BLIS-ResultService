using AutoMapper;
using Bioreference.Common.TestMaster;
using Bioreference.Messaging.Abstractions.DTO;
using Bioreference.ResultService.Abstractions.Application.Audit;
using Bioreference.ResultService.Abstractions.Application.RealTimePending;
using Bioreference.ResultService.Abstractions.Application.Report;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Application.Model.Report.ClinicalTrials;
using Bioreference.ResultService.Application.Model.Report.PriorResult;
using Bioreference.ResultService.Common.Common.Report;
using Bioreference.ResultService.Common.Helpers;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;

namespace Bioreference.ResultService.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly IRealTimePendingService _realTimePendingService;
        private readonly IAuditService _auditService;
        private ILogger<ReportController> _logger;
        private readonly IMapper _mapper;
        public ReportController(IReportService reportService, IRealTimePendingService realTimePendingService,
            IAuditService auditService, ILogger<ReportController> logger, IMapper mapper)
        {
            _reportService = reportService;
            _realTimePendingService = realTimePendingService;
            _auditService = auditService;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> Report([FromQuery] string? accessionNbr, [FromQuery] DateTime? dateServiced, [FromQuery] int? id, [FromHeader(Name = "useCache")] bool? useCache, [FromHeader(Name = "cacheKey")] string? cacheKey)
        {     
            if (!string.IsNullOrWhiteSpace(accessionNbr))
            {
                ActivityHelper.SetAccessionLogKey(accessionNbr.Trim());
                _logger.LogInformation(
                    "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                    accessionNbr.Trim(),
                    DateTime.UtcNow
                );
            }
            else
            {
                ActivityHelper.SetLogKey($"Report:Get:ReportId:{id};UseCache:{useCache};CacheKey:{cacheKey}");
            }

            if (dateServiced == null)
            {
                dateServiced = DateTime.MinValue;
            }
            if (!string.IsNullOrWhiteSpace(accessionNbr))
            {
                _logger.LogInformation("Report Fetch Called");
                _logger.LogInformation(string.Format("Controller {0}, Accesion No: {1}, Date Serviced : {2} ", "ReportController", accessionNbr, dateServiced.Value));
                Application.Model.ReportModel response = await _reportService.ReportFetch(accessionNbr, dateServiced.Value);

                if (response == null)
                    return Ok(
                        $"No report found for accession number {accessionNbr} on " +
                        $"{(dateServiced.Value == DateTime.MinValue
                            ? "no date of service"
                            : dateServiced.Value.ToString("yyyy-MM-ddTHH:mm:ss"))}");

                WebAPI.Model.ReportModel mappedResponse = _mapper.Map<WebAPI.Model.ReportModel>(response);
                return Ok(mappedResponse);
            }
            else if (id.HasValue && id > 0)
            {
                bool useCacheValue = useCache ?? false;
                cacheKey = !string.IsNullOrWhiteSpace(cacheKey) ? cacheKey.Trim() : Guid.NewGuid().ToString();
                Application.Model.ReportModel response = await _reportService.ReportFetchByReportId(id.Value, useCacheValue, cacheKey);

                if (response == null)
                    return Ok(
                        $"No report found for accession number {accessionNbr} on " +
                        $"{(dateServiced.Value == DateTime.MinValue
                            ? "no date of service"
                            : dateServiced.Value.ToString("yyyy-MM-ddTHH:mm:ss"))}");

                WebAPI.Model.ReportModel mappedResponse = _mapper.Map<WebAPI.Model.ReportModel>(response);
                Response.Headers.Append("cacheKey", cacheKey);
                return Ok(mappedResponse);
            }

            return BadRequest("Missing or invalid parameters.");
        }

        [HttpPost("Event")]
        public async Task<IActionResult> ReportUpdate([FromBody] EventResults resultEvents, [FromHeader(Name = "useCache")] bool? useCache, [FromHeader(Name = "cacheKey")] string? cacheKey)
        {
            ActivityHelper.SetLogKey($"Report:Event:UseCache:{useCache};CacheKey:{cacheKey}");
            bool useCacheValue = useCache ?? false;
            cacheKey = !string.IsNullOrWhiteSpace(cacheKey) ? cacheKey.Trim() : Guid.NewGuid().ToString();
            Application.Model.ReportModel response = await _reportService.ResultUpdate(resultEvents, useCacheValue, cacheKey);
            WebAPI.Model.ReportModel mappedResponse = _mapper.Map<WebAPI.Model.ReportModel>(response);            
            Response.Headers.Append("cacheKey", cacheKey);
            return Ok(mappedResponse);
        }

        [HttpPost("SearchPending")]
        public async Task<IActionResult> Search([FromBody] Application.Model.RTPSearchCriteria searchCriteriaDTO)
        {
            ActivityHelper.SetLogKey($"Report:SearchPending:PendingListId:{searchCriteriaDTO?.PendingListId};StartDate:{searchCriteriaDTO?.StartDate:yyyy-MM-dd};EndDate:{searchCriteriaDTO?.EndDate:yyyy-MM-dd};LastHours:{searchCriteriaDTO?.LastHours};ExcludeHours:{searchCriteriaDTO?.ExcludeHours}");
            var response = await _realTimePendingService.RealTimePendingFetch(searchCriteriaDTO);
            return Ok(response);
        }

        [HttpPost("SearchAudit")]
        public async Task<IActionResult> Get([FromBody] WebAPI.Model.AuditSearchCriteria input)
        {
            var accessionNumber = input?.AccessionNumber;
            if (!string.IsNullOrWhiteSpace(accessionNumber))
            {
                ActivityHelper.SetAccessionLogKey(accessionNumber.Trim());
                _logger.LogInformation(
                    "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                    accessionNumber.Trim(),
                    DateTime.UtcNow
                );
            }
            else
            {
                ActivityHelper.SetLogKey($"Report:SearchAudit:ReportId:{input?.ReportId};ServiceDate:{input?.ServiceDate:yyyy-MM-dd}");
            }
            try
            {
                List<Application.Model.AuditModel> result = await _auditService.Search(_mapper.Map<Application.Model.AuditSearchCriteria>(input));
                List<Model.AuditModel> response = _mapper.Map<List<WebAPI.Model.AuditModel>>(result);
                return Ok(response);
            }
            catch(Exception ex)
            {
                throw;
            }
        }

        [HttpPost("ResetRefAnlayte")]
        public async Task<IActionResult> ResetRefAnlayte(string testCode, List<long> reportIds)
        {
            ActivityHelper.SetLogKey($"Report:ResetRefAnalyte:TestCode:{testCode};ReportCount:{reportIds?.Count ?? 0}");
            var response = await _reportService.ResetRefAnlayte(testCode, reportIds);
            return Ok(response);
        }

        [HttpGet("Users")]
        public async Task<IActionResult> GetAuditUsers(int reportId)
        {
            ActivityHelper.SetLogKey($"Report:Users:ReportId:{reportId}");
            var response = await _reportService.GetAuditUsers(reportId);
            return Ok(response);

        }

        [HttpGet("ResponsibleLabs")]
        public async Task<IActionResult> GetResponsibleLabs()
        {
            ActivityHelper.SetLogKey("Report:ResponsibleLabs");
            var response = await _reportService.GetResponsibleLabsDropdown();
            return Ok(response);
        }

        [HttpGet("Significanes")]
        public async Task<IActionResult> GetSignificanes()
        {
            ActivityHelper.SetLogKey("Report:Significanes");
            var response = await _reportService.GetSignificancesDropdown();
            return Ok(response);
        }

        [HttpPost("CorrectedReasons")]
        public async Task<IActionResult> SaveCorrectedReasons(List<Application.Model.CorrectedReasonsModel> correctedReasonList)
        {
            ActivityHelper.SetLogKey($"Report:CorrectedReasons:Count:{correctedReasonList?.Count ?? 0}");
            var response = await _reportService.SaveCorrectedReasons(correctedReasonList);
            return Ok(response);
        }
        [HttpGet("CorrectedReasonMapping")]
        public async Task<IActionResult> CorrectedReasonMapping()
        {
            ActivityHelper.SetLogKey("Report:CorrectedReasonMapping");
            Application.Model.CorrectedReasonMappingsModel response = await _reportService.CorrectedReasonMapping();
            WebAPI.Model.CorrectedReasonMappingsModel mappedResponse = _mapper.Map<WebAPI.Model.CorrectedReasonMappingsModel>(response);
            return Ok(mappedResponse);

        }
        [HttpGet("DepartmentResponsible")]
        public async Task<IActionResult> DepartmentResponsible()
        {
            ActivityHelper.SetLogKey("Report:DepartmentResponsible");
            var response = await _reportService.DepartmentResponsible();
            return Ok(response);

        }
        [HttpGet("ReportingDepartment")]
        public async Task<IActionResult> ReportingDepartment()
        {
            ActivityHelper.SetLogKey("Report:ReportingDepartment");
            var response = await _reportService.ReportingDepartment();
            return Ok(response);

        }
        [HttpPost("SendToCHM")]
        public async Task<IActionResult> SendToCHM(SendToCHMModel sendToCHM)
        {
            ActivityHelper.SetLogKey($"Report:SendToCHM:ReportId:{sendToCHM?.ReportId};DetailCount:{sendToCHM?.Details?.Count ?? 0}");
            var response = await _reportService.SendToCHM(sendToCHM);
            return Ok(new { result = response });

        }

        [HttpPost("ClinicalTrials")]
        public async Task<IActionResult> GetClinicalTrailsData(ClinicalTrialsRequestModel clinicalTrialsRequestModel)
        {
            ActivityHelper.SetLogKey($"Report:ClinicalTrials:StartDate:{clinicalTrialsRequestModel?.StartDate:yyyy-MM-dd};EndDate:{clinicalTrialsRequestModel?.EndDate:yyyy-MM-dd};ClientCount:{clinicalTrialsRequestModel?.ClientID?.Count ?? 0};TestCodeCount:{clinicalTrialsRequestModel?.TestCode?.Count ?? 0}");
            var response = await _reportService.GetClinicalTrailsData(clinicalTrialsRequestModel);
            return Ok(response);

        }

        [HttpGet("ssrs")]
        public async Task<IActionResult> GetSSRSReport([FromQuery] string batchNumber)
        {
            ActivityHelper.SetLogKey($"Report:SSRS:BatchNumber:{batchNumber}");
            var reportUrl = $"https://lis-ssrs.bioreference.com/ReportServer?/LISReports/SummaryReports/LIS_SpecimenBatchRequest_ByDeptRequestDateDetail_qa&rs:Command=Render&BatchNumber={batchNumber}&rs:Format=PDF";

            var handler = new HttpClientHandler
            {
                Credentials = new NetworkCredential("SVCIntranetApp2", "KLnkv6UburYRsh5w", "bioreference-laboratories.com")
            };

            using var client = new HttpClient(handler);
            var response = await client.GetAsync(reportUrl);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "Failed to fetch report.");
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();
            return File(bytes, "application/pdf", "Report.pdf");
        }

        [HttpPost("PriorResults")]
        public async Task<IActionResult> PriorResults([FromBody] PriorResultRequestModel priorResultModel)
        {
            ActivityHelper.SetLogKey($"Report:PriorResults:ReportId:{priorResultModel?.ReportId};Analyte:{priorResultModel?.AnalyteCode};IncludeTNPQNS:{priorResultModel?.IncludeTNPQNS};IncludeAllStatus:{priorResultModel?.IncludeAllStatus}");
            List<PriorResultModel> priorResults = await _reportService.GetPriorResults(priorResultModel);
            var response = _mapper.Map<List<WebAPI.Model.PriorResultModel>>(priorResults);
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            return Ok(JsonConvert.SerializeObject(response, settings));
        }
    }
}
