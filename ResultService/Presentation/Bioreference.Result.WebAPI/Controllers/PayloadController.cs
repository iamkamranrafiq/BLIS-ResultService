using AutoMapper;
using Bioreference.ResultService.Abstractions.Application.Audit;
using Bioreference.ResultService.Abstractions.Application.Order;
using Bioreference.ResultService.Abstractions.Application.Payload;
using Bioreference.ResultService.Abstractions.Application.RealTimePending;
using Bioreference.ResultService.Abstractions.Application.Report;
using Bioreference.ResultService.Application.Model;
using Bioreference.ResultService.Application.Payload;
using Bioreference.ResultService.DI.Interface;
using Flurl.Http.Testing;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Bioreference.ResultService.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PayloadController : ControllerBase
    {
        private readonly IReportService _reportService; 
        private readonly ISSUPayloadService _ssuPayloadService;
        private readonly IORUPayloadService _oruPayloadService;
        private readonly ISTSPayloadService _stsPayloadService;
        private readonly IAuditService _auditService;
        private readonly ILogger<PayloadController> _logger;
        private readonly IMapper _mapper;

        public PayloadController(
            IReportService reportService,
            ISSUPayloadService ssuPayloadService,
            IORUPayloadService oruPayloadService,
            ISTSPayloadService stsPayloadService,
            IAuditService auditService,            
            ILogger<PayloadController> logger,
            IMapper mapper) // inject your mapper here
        {
            _reportService = reportService;
            _ssuPayloadService = ssuPayloadService;
            _oruPayloadService = oruPayloadService;
            _stsPayloadService = stsPayloadService;
            _auditService = auditService;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpPost("SSU")]
        [HttpGet("SSU")]
        public async Task<IActionResult> GetSSU([FromQuery] string? accessionNbr, [FromQuery] DateTime? dateServiced, [FromQuery] bool? withOBX = true)
        {
            if (string.IsNullOrWhiteSpace(accessionNbr) || !dateServiced.HasValue)
                return BadRequest("Missing or invalid parameters.");

            _logger.LogInformation(string.Format("SSU HL7 Payload fetch called for accession {0} on {1}", accessionNbr, dateServiced));

            try
            {
                string[] ssuMessage = await _ssuPayloadService.Message(accessionNbr, dateServiced.Value, withOBX);
                if (ssuMessage == null || ssuMessage.Length == 0)
                {
                    _logger.LogWarning(string.Format("No report found for accession {0}", accessionNbr));
                    return NotFound($"No report found for accession {accessionNbr}");
                }
                if (Request.Method == "POST")
                {
                    return Ok(ssuMessage); 
                }
                else if (Request.Method == "GET")
                {
                    return Content(string.Join(Environment.NewLine + Environment.NewLine, ssuMessage), "text/plain");
                }
                else
                {
                    return Ok("invalid Request.Method : " + Request.Method.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, string.Format("Error while generating HL7 SSU message for accession {0}", accessionNbr));
                return StatusCode((int)HttpStatusCode.InternalServerError, "Failed to process SSU message.");
            }
        }

        [HttpPost("ORU")]
        [HttpGet("ORU")]
        public async Task<IActionResult> GetORU([FromQuery] string? accessionNbr, [FromQuery] DateTime? dateServiced)
        {
            if (string.IsNullOrWhiteSpace(accessionNbr) || !dateServiced.HasValue)
                return BadRequest("Missing or invalid parameters.");

            _logger.LogInformation(string.Format("ORU HL7 Payload fetch called for accession {0} on {1}", accessionNbr, dateServiced));

            try
            {
                string oruMessage = await _oruPayloadService.Message(accessionNbr, dateServiced.Value);
                if (oruMessage == null || oruMessage.Length == 0)
                {
                    _logger.LogWarning(string.Format("No report found for accession {0}", accessionNbr));
                    return NotFound($"No report found for accession {accessionNbr}");
                }
                if (Request.Method == "POST")
                {
                    return Ok(oruMessage);
                }
                else if (Request.Method == "GET")
                {
                    return Content(string.Join(Environment.NewLine + Environment.NewLine, oruMessage), "text/plain");
                }
                else
                {
                    return Ok("invalid Request.Method : " + Request.Method.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, string.Format("Error while generating HL7 ORU message for accession {0}", accessionNbr));
                return StatusCode((int)HttpStatusCode.InternalServerError, "Failed to process ORU message.");
            }
        }

        [HttpPost("STSFunc")]
        [HttpGet("STSFunc")]
        public async Task<IActionResult> GetSTSFunc([FromQuery] string? accessionNbr, [FromQuery] DateTime? dateServiced)
        {
            if (string.IsNullOrWhiteSpace(accessionNbr) || !dateServiced.HasValue)
                return BadRequest("Missing or invalid parameters.");

            _logger.LogInformation(string.Format("STS HL7 Payload fetch called for accession {0} on {1}", accessionNbr, dateServiced));

            try
            {
                //                string stsMessage =
                //                    @"MSH|^~\&|REPORTING-TEST|BRLI|MODULIS-TEST|BRLI|20251117165919||FUNC^ACK|23956841|P|2.5|
                //MSA|CA|20251117165919|108084235^17-NOV-2025^04:59 PM^J252^POSTED
                //MSA|CA|20251117165919|108084235^17-NOV-2025^04:59 PM^J256^POSTED"
                //                    .Replace("108084235", accessionNbr);

                //                if (Request.Method == "POST")
                //                {
                //                    // Return as JSON
                //                    return Ok(new { stsMessage });
                //                }
                //                else if (Request.Method == "GET")
                //                {
                //                    // Return plain text
                //                    return Content(stsMessage, "text/plain");
                //                }
                //                else
                //                {
                //                    return BadRequest("Invalid Request.Method: " + Request.Method);
                //                }

                string stsMessage = await _stsPayloadService.Message(accessionNbr, dateServiced.Value);
                if (stsMessage == null || stsMessage.Length == 0)
                {
                    _logger.LogWarning(string.Format("No report found for accession {0}", accessionNbr));
                    return NotFound($"No report found for accession {accessionNbr}");
                }
                if (Request.Method == "POST")
                {
                    return Ok(stsMessage);
                }
                else if (Request.Method == "GET")
                {
                    return Content(string.Join(Environment.NewLine + Environment.NewLine, stsMessage), "text/plain");
                }
                else
                {
                    return Ok("invalid Request.Method : " + Request.Method.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, string.Format("Error while generating HL7 STS FUNC message for accession {0}", accessionNbr));
                return StatusCode((int)HttpStatusCode.InternalServerError, "Failed to process STS message.");
            }
        }

        
        [HttpPost("STSComplete")]
        [HttpGet("STSComplete")]
        public async Task<IActionResult> GetSTSComplete([FromQuery] string? accessionNbr, [FromQuery] DateTime? dateServiced)
        {
            if (string.IsNullOrWhiteSpace(accessionNbr) || !dateServiced.HasValue)
                return BadRequest("Missing or invalid parameters.");

            _logger.LogInformation(string.Format("STS HL7 Payload fetch called for accession {0} on {1}", accessionNbr, dateServiced));

            try
            {
                //var stsCompletedMessages = new[]
                //{
                //    new
                //    {
                //        AccessionNumber = "10" + accessionNbr,
                //        FinalReportDate = "11/17/2025 02:52:22 PM",
                //        ServiceDate = "11/17/2025 10:31:00 AM",
                //        Status = "COMPLETED"
                //    }
                //};

                //// Convert each object to JSON
                //var lines = stsCompletedMessages
                //    .Select(m => System.Text.Json.JsonSerializer.Serialize(m));


                //if (Request.Method == "POST")
                //{
                //    return Ok(lines);
                //}
                //else if (Request.Method == "GET")
                //{
                //    return Content(string.Join(Environment.NewLine + Environment.NewLine, lines), "text/plain");
                //}
                //else
                //{
                //    return Ok("invalid Request.Method : " + Request.Method.ToString());
                //}
                List<string> stsCompleteMessage = await _stsPayloadService.MessageComplete(accessionNbr, dateServiced.Value);
                if (stsCompleteMessage == null || stsCompleteMessage.Count == 0)
                {
                    _logger.LogWarning(string.Format("No report found for accession {0}", accessionNbr));
                    return NotFound($"No report found for accession {accessionNbr}");
                }
                if (Request.Method == "POST")
                {
                    return Ok(stsCompleteMessage);
                }
                else if (Request.Method == "GET")
                {
                    return Content(string.Join(Environment.NewLine + Environment.NewLine, stsCompleteMessage), "text/plain");
                }
                else
                {
                    return Ok("invalid Request.Method : " + Request.Method.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, string.Format("Error while generating HL7 STS message for accession {0}", accessionNbr));
                return StatusCode((int)HttpStatusCode.InternalServerError, "Failed to process STS message.");
            }
        }
    }
}
