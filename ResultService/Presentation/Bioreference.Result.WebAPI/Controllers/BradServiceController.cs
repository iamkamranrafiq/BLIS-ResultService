using Bioreference.LIS;
using Bioreference.ResultService.Abstractions.Application.Brad;
using Bioreference.ResultService.Abstractions.Application.RealTimePending;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Common.Helpers;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;

namespace Bioreference.ResultService.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BradServiceController : ControllerBase
    {
        private readonly IRealTimePendingService _realTimePendingService;
        private readonly IBradService _bradService;
        public BradServiceController(IRealTimePendingService realTimePendingService, IBradService bradService)
        {
           _realTimePendingService  = realTimePendingService;
            _bradService = bradService; 
        }     

        [HttpPost("SendToBRAD")]
        public async Task<IActionResult> SendToBRAD([FromBody] CallToBradSearchCriteria sendToBradDTO)
        {
            var specimenCount = (sendToBradDTO?.walkInList?.Count ?? 0) + (sendToBradDTO?.nonWalkInList?.Count ?? 0);
            ActivityHelper.SetLogKey($"BRAD:SendToBRAD:SpecimenCount:{specimenCount}");
            var response = await _bradService.SendToBRAD(sendToBradDTO);
            return Ok(response);

        }
        [HttpPost("GetMappedSpecimens")]
        public async Task<IActionResult> GetMappedSpecimens([FromBody] List<PendingBradSearchCriteria> SpecList)
        {
            ActivityHelper.SetLogKey($"BRAD:GetMappedSpecimens:Count:{SpecList?.Count ?? 0}");
            var response = await _bradService.GetMappedSpecimens(SpecList);
            return Ok(response);
        }
        [HttpPost("GetMappedAccessions")]
        public async Task<IActionResult> GetMappedAccessions(List<List<string>> accessions, string fridgeId)
        {
            ActivityHelper.SetLogKey($"BRAD:GetMappedAccessions:FridgeId:{fridgeId};GroupCount:{accessions?.Count ?? 0}");
            var response = await _bradService.GetMappedAccessions(accessions, fridgeId);
            return Ok(response);
        }
        [HttpPost("CheckRequestable")]
        public async Task<IActionResult> CheckRequestable([FromBody] List<PendingBradSearchCriteria> SpecList)
        {
            ActivityHelper.SetLogKey($"BRAD:CheckRequestable:Count:{SpecList?.Count ?? 0}");
            var response = await _bradService.CheckRequestable(SpecList);
            return Ok(response);
        }
        [HttpGet("SpecimenTypeList")]
        public async Task<IActionResult> GetSpecimenTypeList()
        {
            ActivityHelper.SetLogKey("BRAD:SpecimenTypeList");
            var response = await _bradService.GetSpecimenTypeList();
            return Ok(response);
        }

        [HttpGet("WalkInFridges")]
        public async Task<IActionResult> GetWalkInFridges()
        {
            ActivityHelper.SetLogKey("BRAD:WalkInFridges");
            var response = await _bradService.GetWalkInFridges();
            return Ok(response);
        }

        [HttpGet("Fridge")]
        public async Task<IActionResult> GetFridge()
        {
            ActivityHelper.SetLogKey("BRAD:Fridge");
            var response = await _bradService.GetFridge();
            var dict = new List<DictionaryEntry>();
            dict.Add(new DictionaryEntry(0, "Select"));
            foreach (var fridgeInfo in response)
            {
                dict.Add(new DictionaryEntry(fridgeInfo.FridgeID, fridgeInfo.FridgeNumber));
            }           
            return Ok(dict);
        }

    }
}
