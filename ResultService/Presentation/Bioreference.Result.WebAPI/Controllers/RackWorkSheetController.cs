using AutoMapper;
using Bioreference.ResultService.Abstractions.Application.WorkSheet;
using Bioreference.ResultService.Common.Helpers;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Bioreference.Result.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RackWorkSheetController : ControllerBase
    {
        private readonly IWorkSheetService _workSheetService;
        private readonly IMapper _mapper;
        private readonly ILogger<RackWorkSheetController> _logger;
        public RackWorkSheetController(ILogger<RackWorkSheetController> logger, IWorkSheetService workSheetService, IMapper mapper)
        {
            _workSheetService = workSheetService;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> RackWorkSheet(int workSheetId, int pageNo, int pageSize)
        {
            ActivityHelper.SetLogKey($"RackWorkSheet:Get:WorkSheetId:{workSheetId};PageNo:{pageNo};PageSize:{pageSize}");

            List<ResultService.Application.Model.WorkSheetReportModel> result = await _workSheetService.GetWorkSheetById(workSheetId, pageNo, pageSize);
            var response = _mapper.Map<List<ResultService.WebAPI.Model.WorkSheetReportModel>>(result);
            return Ok(JsonConvert.SerializeObject(response));
        }

        [HttpPost("Search")]
        public async Task<IActionResult> GetRackWorkSheets([FromBody] ResultService.Application.Model.WorkSheetSearchCriteria workSheetSearchRequest)
        {
            ActivityHelper.SetLogKey($"RackWorkSheet:Search:TemplateId:{workSheetSearchRequest?.TemplateId};DateFrom:{workSheetSearchRequest?.DateFrom:yyyy-MM-dd};DateTo:{workSheetSearchRequest?.DateTo:yyyy-MM-dd};PageNo:{workSheetSearchRequest?.PageNumber};PageSize:{workSheetSearchRequest?.PageSize}");

            List<ResultService.Application.Model.RackWorkSheetModel> result = await _workSheetService.GetWorkSheets(workSheetSearchRequest);
            var response = _mapper.Map<List<ResultService.WebAPI.Model.RackWorkSheetModel>>(result);
            return Ok(JsonConvert.SerializeObject(response));
        }

        [HttpPost("Release")]
        public async Task<IActionResult> ReleaseWorkSheet(List<int> reportIds, int templateId, int worksheetID)
        {
            var reportCount = reportIds?.Count ?? 0;
            var firstReportId = reportCount > 0 ? reportIds[0] : 0;
            ActivityHelper.SetLogKey($"RackWorkSheet:Release:WorkSheetId:{worksheetID};TemplateId:{templateId};ReportCount:{reportCount};FirstReportId:{firstReportId}");

            var response = await _workSheetService.ReleaseCheckedWorkSheets(reportIds, templateId, worksheetID);
            return Ok(new { result = response });
        }

        [HttpPost("Save")]
        public async Task<IActionResult> SaveWorkSheet([FromBody] ResultService.Application.Model.WorkSheet.WorkSheetAddUpdateModel workSheetSaveModel)
        {
            
            var accession = workSheetSaveModel?.WorkSheetSpecimen?.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.AccessionNo))?.AccessionNo;
            if (!string.IsNullOrWhiteSpace(accession))
            {
                ActivityHelper.SetAccessionLogKey(accession.Trim());
                _logger.LogInformation(
                    "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                    accession.Trim(),
                    DateTime.UtcNow
                );
            }
            else
            {
                ActivityHelper.SetLogKey($"RackWorkSheet:Save:WorkSheetId:{workSheetSaveModel?.Id};TemplateId:{workSheetSaveModel?.RackWorkSheetTemplateId};SpecimenCount:{workSheetSaveModel?.WorkSheetSpecimen?.Count ?? 0}");
            }

            var response = await _workSheetService.SaveWorksheet(workSheetSaveModel);
            return Ok(new { result = response });
        }

        [HttpPut("Update")]
        public async Task<IActionResult> UpdateWorkSheet([FromBody] ResultService.Application.Model.WorkSheet.WorkSheetAddUpdateModel workSheetSaveModel)
        {
            var accession = workSheetSaveModel?.WorkSheetSpecimen?.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.AccessionNo))?.AccessionNo;
            if (!string.IsNullOrWhiteSpace(accession))
            {
                ActivityHelper.SetAccessionLogKey(accession.Trim());
                _logger.LogInformation(
                    "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                    accession.Trim(),
                    DateTime.UtcNow
                );
            }
            else
            {
                ActivityHelper.SetLogKey($"RackWorkSheet:Update:WorkSheetId:{workSheetSaveModel?.Id};TemplateId:{workSheetSaveModel?.RackWorkSheetTemplateId};SpecimenCount:{workSheetSaveModel?.WorkSheetSpecimen?.Count ?? 0}");
            }

            var response = await _workSheetService.UpdateWorksheet(workSheetSaveModel);
            return Ok(new { result = response });
        }
        [HttpGet("Templates")]
        public async Task<IActionResult> GetRackWorkSheetTemplates()
        {
            ActivityHelper.SetLogKey("RackWorkSheet:Templates");

            var response = await _workSheetService.GetWorkSheetsTemplates();
            return Ok(response);
        }

        [HttpPut("Archieve")]
        public async Task<IActionResult> ArchieveRackWorksheet(int workSheetId)
        {
            ActivityHelper.SetLogKey($"RackWorkSheet:Archive:WorkSheetId:{workSheetId}");

            var response = await _workSheetService.ArchieveWorkSheet(workSheetId);
            return Ok(new { result = response });
        }

        [HttpPost("IsAddSpecimen")]
        public async Task<IActionResult> IsAddSpecimen([FromBody] ResultService.Application.Model.WorkSheet.WorkSheetSpecimenModel workSheetSpecimenModel, int templateId)
        {

            if (!string.IsNullOrWhiteSpace(workSheetSpecimenModel.AccessionNo))
            {
                ActivityHelper.SetAccessionLogKey(workSheetSpecimenModel.AccessionNo.Trim());
                _logger.LogInformation(
                    "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                    workSheetSpecimenModel.AccessionNo.Trim(),
                    DateTime.UtcNow
                );
            }
            else
            {
                ActivityHelper.SetLogKey($"RackWorkSheet:IsAddSpecimen:TemplateId:{templateId};RackId:{workSheetSpecimenModel.RackId};RackPosition:{workSheetSpecimenModel?.RackPosition};SpecimenId:{workSheetSpecimenModel?.ID}");
            }

            var response = await _workSheetService.IsAddSpecimen(workSheetSpecimenModel,templateId);
            return Ok(response);
        }

        [HttpGet("IsWorksheetArchieved")]
        public async Task<IActionResult> IsWorksheetArchieved(int workSheetId)
        {
            ActivityHelper.SetLogKey($"RackWorkSheet:IsArchived:WorkSheetId:{workSheetId}");

           var response = await _workSheetService.IsWorksheetArchieved(workSheetId);
            return Ok(JsonConvert.SerializeObject(response));
        }
    }
}
