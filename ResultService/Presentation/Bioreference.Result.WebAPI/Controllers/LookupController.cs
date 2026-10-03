using AutoMapper;
using Bioreference.Common.TestMaster;
using Bioreference.ResultService.Abstractions.Application.Lookup;
using Bioreference.ResultService.Abstractions.Application.RapidResult;
using Bioreference.ResultService.Abstractions.Application.Setting;
using Bioreference.ResultService.Common.Helpers;
using Bioreference.ResultService.WebAPI.Model;
using Microsoft.AspNetCore.Mvc;


namespace Bioreference.ResultService.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LookupController : Controller
    {
        private readonly ILookupService _lookupService;
        private readonly IRapidResultService _rapidResultService;
        private readonly ISettingService _settingService;
        private readonly IMapper _mapper;
        public LookupController(ILookupService lookupService, IRapidResultService rapidResultService, ISettingService settingService, IMapper mapper)
        {
            _lookupService = lookupService;
            _rapidResultService = rapidResultService;
            _settingService = settingService;  
            _mapper = mapper;
        }

        [HttpGet("TestGroups")]
        public async Task<IActionResult> GetTestCodeGroups()
        {
            ActivityHelper.SetLogKey("Lookup:TestGroups");
            var result = await _lookupService.GetTestCodeGroups(false, true);
            var response = _mapper.Map<TestCodeGroupsModel>(result);
            return Ok(response.List.TestCodeGroup);         
        }        
      
        [HttpGet("Comments")]
        public async Task<IActionResult> GetComments(CommentTypeModel commentType)
        {
            ActivityHelper.SetLogKey($"Lookup:Comments:CommentType:{commentType}");
            var result = await  _lookupService.GetComments((Application.Model.Enum.CommentTypeModel)commentType);
            var response = _mapper.Map<List<WebAPI.Model.CommentModel>>(result);
            return Ok(response);
        } 

        [HttpGet("Divisions")]
        public async Task<IActionResult> GetDivisions()
        {
            ActivityHelper.SetLogKey("Lookup:Divisions");
            var response = await _lookupService.GetDivisions();
            return Ok(response);
        }

        [HttpGet("PendingList")]
        public async Task<IActionResult> GetPendingList()
        {
            ActivityHelper.SetLogKey("Lookup:PendingList");
            var response = await _lookupService.GetPendingList();
            return Ok(response);
        } 

        [HttpGet("ResultSettings")]
        public async Task<IActionResult> GetResultSettings()
        {
            ActivityHelper.SetLogKey("Lookup:ResultSettings");
            var response = await _settingService.GetResultSettings();   
            return Ok(response);
        }

        [HttpGet("PendingListItems")]
        public async Task<IActionResult> GetPendingListItems(int id)
        {
            ActivityHelper.SetLogKey($"Lookup:PendingListItems:Id:{id}");
            var response = await _lookupService.GetPendingListItems(id);
            return Ok(response);
        }
    }
}
