using AutoMapper;
using Bioreference.ResultService.Abstractions.Application.Order;
using Bioreference.ResultService.Abstractions.Application.Processors;
using Bioreference.ResultService.Common.Helpers;
using Bioreference.ResultService.WebAPI.Model;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Diagnostics;

namespace Bioreference.Result.WebAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IOrderSearchService _orderSearchService;
        private readonly IMapper _mapper;
        private readonly IQueueOrderInProcessor _queueOrderInProcessor;
        private readonly ILogger<OrderController> _logger;
        private readonly IConfiguration _configuration;

        public OrderController(
            IOrderService orderService, 
            IOrderSearchService orderSearchService, 
            IMapper mapper,
            IQueueOrderInProcessor queueOrderInProcessor,
            ILogger<OrderController> logger,
            IConfiguration configuration)
        {
            _orderService = orderService;
            _orderSearchService = orderSearchService;
            _mapper = mapper;
            _queueOrderInProcessor = queueOrderInProcessor;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? accessionNbr, [FromQuery] int? id, [FromQuery] DateTime? dateServiced)
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
                ActivityHelper.SetLogKey($"Order:Get:Id:{id};DateServiced:{dateServiced:yyyy-MM-dd}");
            }

            if (!string.IsNullOrWhiteSpace(accessionNbr) && !dateServiced.HasValue)
            {
                ResultService.Application.Model.OrderModel result = await _orderService.FetchOrder(accessionNbr);
                var response = _mapper.Map<ResultService.WebAPI.Model.OrderModel>(result);
                return Ok(response);
            }
            else if (id.HasValue)
            {
                ResultService.Application.Model.OrderModel result = await _orderService.FetchOrder(id.Value);
                var response = _mapper.Map<ResultService.WebAPI.Model.OrderModel>(result);
                return Ok(response);
            }
            else if (dateServiced.HasValue && !string.IsNullOrWhiteSpace(accessionNbr))
            {
                ResultService.Application.Model.OrderModel result = await _orderService.FetchOrder(accessionNbr, dateServiced.Value);
                var response = _mapper.Map<ResultService.WebAPI.Model.OrderModel>(result);
                return Ok(response);
            }

            return BadRequest("Missing or invalid parameters.");
        }

        [HttpPost("Search")]
        public async Task<IActionResult> Search([FromBody] ResultService.Application.Model.OrderSearchCriteria input)
        {
            ActivityHelper.SetLogKey($"Order:Search:Accession:{input?.AccessionNumber};Account:{input?.AccountNumber};StartDate:{input?.StartDate:yyyy-MM-dd};EndDate:{input?.EndDate:yyyy-MM-dd};EUID:{input?.EUID}");
            List<ResultService.Application.Model.OrderSearchModel> result = await _orderSearchService.Search(input);
            var response = _mapper.Map<List<ResultService.WebAPI.Model.OrderSearchModel>>(result);
            return Ok(response);
        }
        [HttpGet("OrderComment")]
        public async Task<IActionResult> OrderComment(int orderId)
        {
            ActivityHelper.SetLogKey($"Order:OrderComment:Get:OrderId:{orderId}");
           var response = await _orderService.GetOrderComment(orderId);
            return Ok(response);
        }

        [HttpPost("OrderComment")]
        public async Task<IActionResult> OrderComment([FromBody] ResultService.Application.Model.OrderCommentInputModel input)
        {
            if (input == null)
            {
                return BadRequest("Invalid input for order comment.");
            }

            ActivityHelper.SetLogKey($"Order:OrderComment:Save:OrderId:{input.OrderId};CriteriaCount:{input.Criteria?.Count ?? 0}");

            var response = await _orderService.SaveOrderComment(input);
            return Ok(response);
        }

        [HttpGet("Orders")]
        public async Task<IActionResult> FetchOrders(string accessionNbr)
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
                ActivityHelper.SetLogKey("Order:FetchOrders");
            }
            if (!string.IsNullOrWhiteSpace(accessionNbr))
            {
                var response = await _orderService.FetchOrders(accessionNbr);
                return Ok(response);
            }
            else
            {
                return BadRequest("Missing or invalid parameters.");
            }
        }

        [HttpPost("ProcessMessage")]
        public async Task<IActionResult> ProcessMessage([FromBody] ProcessOrderModel messageBody)
        {
            var requestAccession = messageBody?.message?.AccessionNumber;
            if (!string.IsNullOrWhiteSpace(requestAccession))
            {
                ActivityHelper.SetAccessionLogKey(requestAccession.Trim());
                _logger.LogInformation(
                    "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                    requestAccession.Trim(),
                    DateTime.UtcNow
                );
            }
            else
            {
                ActivityHelper.SetLogKey($"Order:ProcessMessage:MessageId:{messageBody?.message?.MessageId}");
            }

            if (messageBody?.message == null || string.IsNullOrWhiteSpace(messageBody.message.Payload))
            {
                return BadRequest("Message or Payload cannot be null.");
            }

            try
            {
                var message = messageBody.message;

                if (!string.IsNullOrWhiteSpace(message.AccessionNumber))
                {
                    ActivityHelper.SetAccessionLogKey(message.AccessionNumber.Trim());
                    _logger.LogInformation(
                        "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                        message.AccessionNumber.Trim(),
                        DateTime.UtcNow
                    );
                }

                var settings = new JsonSerializerSettings { Converters = { new StringEnumConverter() } };
                Contracts.Order.Order order = JsonConvert.DeserializeObject<Contracts.Order.Order>(messageBody.message.Payload, settings);

                if (order == null)
                {
                    return BadRequest("Failed to deserialize order from payload.");
                }

                var stopWatch = Stopwatch.StartNew();
                var response = await _queueOrderInProcessor.ProcessMessage(order, messageBody.processOrderConfig);
                stopWatch.Stop();

                if (response)
                {
                    _logger.LogInformation(string.Format(
                        "Entity: {0}; Event: {1}; Message: {2}; Accession: {3}; ElapsedTime: {4}",
                        "Order", "Success", "Order processing completed for calling application " + order.CallingApplication + ".", order.AccessionNumber, stopWatch.ElapsedMilliseconds));

                    return Ok(new { message = "Order processed successfully", accessionNumber = order.AccessionNumber });
                }
                else
                {
                    _logger.LogError(string.Format(
                       "Entity: {0}; Event: {1}; Message: {2}; Accession: {3}; ElapsedTime: {4}",
                       "Order", "Failed", "Order processing Failed for calling application " + order.CallingApplication + ".", order.AccessionNumber, stopWatch.ElapsedMilliseconds));

                    return StatusCode(500, new { error = "Order Processing Failed" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, string.Format(
                    "Entity: {0}; Event: {1}; Message: {2}; Accession: {3}; ErrorMessage: {4}",
                    "Order", "Exception", "Exception occurred while processing Order.", messageBody?.message?.AccessionNumber ?? "Unknown", ex.Message));

                return StatusCode(500, new { error = "An error occurred while processing the order", message = ex.Message });
            }
        }
    }
}
