using Bioreference.LIS;
using Bioreference.Messaging.Abstractions.DTO;
using Bioreference.Messaging.Abstractions.Enums;
using Bioreference.Messaging.Kafka;
using Bioreference.ResultService.Abstractions.Application.Processors;
using Bioreference.ResultService.Common;
using Bioreference.ResultService.Common.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Diagnostics;
using System.Text;
using static Bioreference.ResultService.Processors.ProcessOrder;

namespace Bioreference.ResultService.Processors
{
    public class ProcessOrder : Bioreference.Messaging.Kafka.BaseConsumerProcessor<ProcessOrder.OrderMessage>
    {
        private readonly ILogger<ProcessOrder> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IQueueOrderInProcessor _queueOrderInProcessor;

        public ProcessOrder(ILogger<ProcessOrder> logger, HttpClient httpClient, IConfiguration configuration, IQueueOrderInProcessor queueOrderInProcessor)
            : base(logger)
        {
            _logger = logger;
            _httpClient = httpClient;
            _configuration = configuration;
            _queueOrderInProcessor = queueOrderInProcessor;
        }

        public override ConsumerProcessingResult ProcessMessage(MessageContext<OrderMessage> messageContext)
        {
            try
            {
                var dest = messageContext.Message.Payload.Queue.Destination;
                if (dest == "B2")
                {
                    ActivityHelper.SetAccessionLogKey(messageContext.Message.Payload.AccessionNumber);
                    _logger.LogInformation(
                    "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                    messageContext.Message.Payload.AccessionNumber,
                    DateTime.UtcNow);
                    bool queueOrderInStatus = _queueOrderInProcessor.QueueOrderInStatus(Convert.ToInt64(messageContext.Message.Payload.OrderHistoryId), 6).GetAwaiter().GetResult();
                    if (!queueOrderInStatus)
                    {
                        _logger.LogInformation(
                        "ProcessOrder consumer is disabled via configuration. Skipping message processing. AccessionNumber: {AccessionNumber}; OrderId: {OrderId}; OrderHistoryId: {OrderHistoryId}",
                        messageContext.Message.Payload.AccessionNumber,
                        messageContext.Message.Payload.OrderId,
                        messageContext.Message.Payload.OrderHistoryId);
                        return ConsumerProcessingResult.Success();
                    }
                    var stopWatch = Stopwatch.StartNew();
                   
                    var processOrderModel = BuildProcessOrderModel(messageContext);
                    var response = CallProcessOrderApi(processOrderModel);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = response.Content.ReadAsStringAsync().Result;
                        _logger.LogError(string.Format("Entity: {0}; Event: {1}; Message: {2}; Accession: {3}; StatusCode: {4}; Error: {5}",
                            "Order", "Update", "Failed to process order.", messageContext.Message.Payload.AccessionNumber, response.StatusCode, errorContent));
                        return ConsumerProcessingResult.Failure($"API call failed with status {response.StatusCode}: {errorContent}");
                    }

                    stopWatch.Stop();
                    _logger.LogInformation(string.Format("Entity: {0}; Event: {1}; Message: {2}; Accession: {3}; ElapsedTime: {4}",
                        "Order", "Success", "Order processing completed.", messageContext.Message.Payload.AccessionNumber, stopWatch.ElapsedMilliseconds));
                }
                return ConsumerProcessingResult.Success();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, string.Format("Entity: {0}; Event: {1}; Message: {2}; ErrorMessage: {3}",
                    "Order", "Exception", "Exception occurred while processing OrderMessage.", ex.Message));
                return ConsumerProcessingResult.Failure(ex.Message);
            }
        }
        private ProcessOrderModel BuildProcessOrderModel(MessageContext<OrderMessage> messageContext)
        {
            OrderMessage message = messageContext.Message.Payload;

            return new ProcessOrderModel
            {
                message = message,
                processOrderConfig = new ProcessOrderConfiguration
                {
                    ToFollowEnabled = _configuration.GetSection("Bioreference.LIS").GetValue<bool>("ToFollowEnabled"),
                    LoggingName = _configuration.GetSection("Bioreference.ConsumerLogging").GetValue<string>("LoggingName") ?? string.Empty
                }
            };
        }
        private HttpResponseMessage CallProcessOrderApi(ProcessOrderModel processOrderModel)
        {
            var resultHost = Environment.GetEnvironmentVariable("RESULT_HOST") ?? "result.blis-services";
            var apiUrl = $"http://{resultHost}/Order/ProcessMessage";

            var settings = new JsonSerializerSettings
            {
                Converters = { new StringEnumConverter() }
            };

            var messageJson = JsonConvert.SerializeObject(processOrderModel, settings);
            var content = new StringContent(messageJson, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Content = content
            };

            request.Headers.Add("X-Username", Thread.CurrentPrincipal?.Identity?.Name ?? "Unknown");
            request.Headers.Add("X-Roles", "");

            return _httpClient.SendAsync(request).Result;
        }


        public class OrderMessage
        {
            public string AccessionNumber { get; set; }
            public string OrderId { get; set; }
            public string QueueRouteId { get; set; }
            public string OrderHistoryId { get; set; }
            public QueueInfo Queue { get; set; }
            public string MessageId { get; set; }
            public string Payload { get; set; }
        }

        public class QueueInfo
        {
            public string QueueName { get; set; }
            public string QueueId { get; set; }
            public string Destination { get; set; }
            public string InternalFlag { get; set; }
        }
    } 
    public class ProcessOrderModel
    {
        public OrderMessage message { get; set; }
        public ProcessOrderConfiguration processOrderConfig { get; set; }
    }

}


