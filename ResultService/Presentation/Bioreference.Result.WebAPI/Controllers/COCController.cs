using AutoMapper;
using Bioreference.ResultService.Abstractions.Application.COC;
using Bioreference.ResultService.Abstractions.Application.Tools;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Application.Model.COC;
using Bioreference.ResultService.Application.Tools;
using Bioreference.ResultService.Common.Helpers;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Bioreference.ResultService.WebAPI.Controllers
{
    public class COCController : Controller
    {
        private readonly ICOCService _cocService;
        private readonly IMapper _mapper;
        private readonly ILogger<COCController> _logger;

        public COCController(ILogger<COCController> logger, ICOCService cocService, IMapper mapper)
        {
            _cocService = cocService;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpPost("COCBatch")]
        public async Task<IActionResult> GetCOCBatches(bool isShowClosed, int pageNo, int pageSize)
        {
            ActivityHelper.SetLogKey($"COC:GetBatch:IsShowClosed:{isShowClosed};PageNo:{pageNo};PageSize:{pageSize}");
            
            var result = await _cocService.FetchCOCReport(isShowClosed, pageNo, pageSize);
            var response = _mapper.Map<List<WebAPI.Model.COCBatchReportModel>>(result);
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            
            return Ok(JsonConvert.SerializeObject(response, settings));

        }

        [HttpPost("COCBatchById")]
        public async Task<IActionResult> COCBatchById(int batchId)
        {
            ActivityHelper.SetLogKey($"COC:BatchId:{batchId}");

            var result = await _cocService.FetchCOCReportItem(batchId);
            var response = _mapper.Map<List<WebAPI.Model.COCBatchReportItemModel>>(result);
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            return Ok(JsonConvert.SerializeObject(response, settings));

        }

        [HttpPut("CloseReOpenBatch")]
        public async Task<IActionResult> CloseReOpenBatch(bool isClosed, int batchId)
        {
            ActivityHelper.SetLogKey($"COC:CloseReOpen:BatchId:{batchId};IsClosed:{isClosed}");

            var result = await _cocService.ClosedReOpenBatch(isClosed,batchId);
            var response = _mapper.Map<WebAPI.Model.CocBatchAccessionEditStatusModel>(result);
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            return Ok(JsonConvert.SerializeObject(response, settings));

        }

        [HttpPut("ReleaseBatch")]
        public async Task<IActionResult> ReleaseBatch(List<int> reports)
        {
            var reportCount = reports?.Count ?? 0;
            var firstReportId = reportCount > 0 ? reports[0] : 0;
            ActivityHelper.SetLogKey($"COC:ReleaseBatch:ReportCount:{reportCount};FirstReportId:{firstReportId}");

            var response = await _cocService.ReleaseBatch(reports);
            return Ok(new { result = response });
        }

        [HttpGet("Delete")]
        public async Task<IActionResult> DeleteCOC(string accessionNumber, int batchId)
        {
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
                ActivityHelper.SetLogKey($"COC:Delete:BatchId:{batchId}");
            }

            var result = await _cocService.RemoveCOC(accessionNumber,batchId);
            var response = _mapper.Map<WebAPI.Model.CocBatchAccessionEditStatusModel>(result);
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            return Ok(JsonConvert.SerializeObject(response, settings));
        }

        [HttpPost("Add")]
        public async Task<IActionResult> AddCOC([FromBody] COCSearchCriteria cOCSearchCriteria)
        {

            ActivityHelper.SetLogKey($"COC:Add:BatchId:{cOCSearchCriteria.BatchId};Date:{cOCSearchCriteria?.Year}-{cOCSearchCriteria?.Month}-{cOCSearchCriteria?.Day};Seq:{cOCSearchCriteria?.SequenceStart}-{cOCSearchCriteria?.SequenceEnd}");

            var result = await _cocService.CreateCOCSequence(cOCSearchCriteria);
            var response = _mapper.Map<WebAPI.Model.COCAddEditResponseModel>(result);
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            return Ok(JsonConvert.SerializeObject(response, settings));
        }

        [HttpPost("AddAccession")]
        public async Task<IActionResult> AddAccession(string accessionNumber, int batchId)
        {
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
                ActivityHelper.SetLogKey($"COC:AddAccession:BatchId:{batchId}");
            }

            var result = await _cocService.AddCOCAccession(accessionNumber, batchId);
            var response = _mapper.Map<WebAPI.Model.CocBatchAccessionEditStatusModel>(result);
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            return Ok(JsonConvert.SerializeObject(response, settings));
        }

        [HttpPost("Update")]
        public async Task<IActionResult> UpdateCOC([FromBody] COCSearchCriteria cOCSearchCriteria)
        {

            ActivityHelper.SetLogKey($"COC:Update:BatchId:{cOCSearchCriteria.BatchId};Date:{cOCSearchCriteria?.Year}-{cOCSearchCriteria.Month}-{cOCSearchCriteria?.Day};Seq:{cOCSearchCriteria?.SequenceStart}-{cOCSearchCriteria?.SequenceEnd}");

            var result = await _cocService.ModifyCOCSequence(cOCSearchCriteria);
            var response = _mapper.Map<WebAPI.Model.COCAddEditResponseModel>(result);
            var settings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            return Ok(JsonConvert.SerializeObject(response, settings));
        }

        [HttpGet("IsCocApprover")]
        public async Task<IActionResult> IsCocApprover(string userName)
        {
            ActivityHelper.SetLogKey($"COC:IsCocApprover:User:{(string.IsNullOrWhiteSpace(userName) ? "Unknown" : userName.Trim())}");

            var response = await _cocService.IsCocApprover(userName);
            return Ok(new { result = response });
        }
    }
}
