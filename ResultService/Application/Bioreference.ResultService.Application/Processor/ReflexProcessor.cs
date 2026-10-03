using Bioreference.Contracts.Result;
using Bioreference.ResultService.Abstractions.Application.Processor;
using Bioreference.ResultService.Abstractions.Application.Setting;
using Bioreference.ResultService.Application.Model.AppSettings.Reflex;
using Bioreference.ResultService.Common.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using Newtonsoft.Json;
using System.Text;
using Bioreference.LIS;

namespace Bioreference.ResultService.Application.Processor
{
    public class ReflexProcessor : IReflexProcessor
    {
        private ILogger<ReflexProcessor> logger;
        private readonly ISettingService settingsProvider;
        private readonly AppSettingsReflex appSettings;
        private ReflexSettings dbSettings;

        public ReflexProcessor(ILogger<ReflexProcessor> logger, ISettingService settingsProvider, IOptions<AppSettingsReflex> options)
        {
            this.logger = logger;
            this.settingsProvider = settingsProvider;
            this.appSettings = options.Value;
        }
        public async Task OnReflexMessage(Reflex msg)
        {
            try
            {
                string connection = settingsProvider.GetConnectionString("Bioreference.LIS");

                dbSettings = await settingsProvider.FetchSetting<ReflexSettings>(
                    connection,
                    "B2ReflexesToSPM",
                    ""
                );

                ActivityHelper.SetAccessionLogKey(msg.AccessionNumber);

                logger.LogInformation(
                    "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                    msg.AccessionNumber,
                    DateTime.UtcNow
                );

                logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; AccessionNumber: {AccessionNumber}; OrderedCode: {OrderedCode}; ReflexCode: {ReflexCode}",
                    "Reflex",
                    "Received",
                    msg.AccessionNumber,
                    msg.OrderedCode,
                    msg.ReflexCode
                );

                string accessionNumber = msg.AccessionNumber.Trim();

                if (accessionNumber.Length == 7)
                    accessionNumber = $"10{accessionNumber}";

                B2AddOnData b2Data = new B2AddOnData();

                B2AddOnTest testData = new B2AddOnTest
                {
                    TestCode = msg.ReflexCode,
                    AccessionNumber = accessionNumber,
                    DateOrdered = msg.DateOrdered,
                    ActionType = ActionTypeEnum.Add,
                    Metadata =
                    [
                        new MetaData
                {
                    Name = "IsB2ReflexTest",
                    Value = "True"
                }
                    ],
                    TestSource = "",
                    TestStatus = "",
                    TestName = "",
                    Comments = "",
                    DateCollected = ""
                };

                b2Data.Tests.Add(testData);

                string json = JsonConvert.SerializeObject(
                    b2Data,
                    Newtonsoft.Json.Formatting.Indented
                );

                logger.LogDebug(
                    "Entity: {Entity}; Event: {Event}; AccessionNumber: {AccessionNumber}; PayloadLength: {PayloadLength}",
                    "Reflex",
                    "BLISUpdateOrderPayload",
                    accessionNumber,
                    json.Length
                );

                string url =
                    $"{dbSettings.BlisETSUrl}" +
                    $"?callingApp=B2_REFLEXES" +
                    $"&userName=EntB2Reflexes" +
                    $"&AccessionNumber={Uri.EscapeDataString(accessionNumber)}";

                using HttpClient client = new HttpClient();

                using StringContent httpContent = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"
                );

                using HttpResponseMessage response = await client.PostAsync(url, httpContent);

                string jsonResponse = await response.Content.ReadAsStringAsync();

                logger.LogDebug(
                    "Entity: {Entity}; Event: {Event}; AccessionNumber: {AccessionNumber}; StatusCode: {StatusCode}; ResponseLength: {ResponseLength}",
                    "Reflex",
                    "BLISUpdateOrderResponse",
                    accessionNumber,
                    response.StatusCode,
                    jsonResponse?.Length ?? 0
                );

                response.EnsureSuccessStatusCode();

                Result result = JsonConvert.DeserializeObject<Result>(jsonResponse);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Entity: {Entity}; Event: {Event}; AccessionNumber: {AccessionNumber}; Message: {Message}",
                    "Reflex",
                    "Error",
                    msg?.AccessionNumber,
                    "Error while processing Reflex message."
                );

                throw;
            }
        }
    }

    //< Serializable() >
    public class B2AddOnTest
    {
        //[XmlElement]
        public string TestSource { get; set; }
        public string TestStatus { get; set; }
        public ActionTypeEnum ActionType { get; set; }
        public string TestCode { get; set; }
        public string TestName { get; set; }
        public string Comments { get; set; }
        public string DateOrdered { get; set; }
        public string DateCollected { get; set; }
        public string AccessionNumber { get; set; }
        public List<MetaData> Metadata { get; set; }
    }

    //< System.Serializable() >
    //< XmlInclude(GetType(B2AddOnTest)) >
    public class B2AddOnData
    {
        public List<B2AddOnTest> Tests { get; set; }
        public B2AddOnData()
        {
            Tests = new List<B2AddOnTest>();
        }
    }

    public class MetaData
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    //<Serializable()>
    sealed class ValidationMessage
    {
        public string Message { get; set; }
        public bool Warning { get; set; }
    }

    //< Serializable() >
    class Result
    {
        public string Status { get; set; }
        public List<ValidationMessage> Messages { get; set; }
        public Result()
        {
            Status = string.Empty;
            Messages = [];
        }
    }

    public enum ActionTypeEnum
    {
        Unknown = 0,
        Add = 1,
        Update = 2,
        Delete = 3,
        AddDeleted = 4
    }
}


//namespace Bioreference.ResultService.Processors
//{
//    public class ReflexConsumer : Bioreference.Messaging.Kafka.BaseConsumerProcessor<Reflex>
//    {
//        ILogger logger;
//        IConfiguration settings;
//        IConfiguration reflexSettings;

//        public ReflexConsumer(ILogger<BaseConsumerProcessor<Reflex>> logger) : base(logger)
//        {
//            this.logger = logger;
//            try
//            {
//                string configFileDirectory = Environment.GetEnvironmentVariable("CONFIG_DIR");
//                if (String.IsNullOrEmpty(configFileDirectory))
//                    configFileDirectory = Directory.GetCurrentDirectory();
//                else
//                    configFileDirectory = Path.GetFullPath(configFileDirectory);
//                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Live";
//                var builder = new ConfigurationBuilder()
//                    .SetBasePath(configFileDirectory)
//                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
//                    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true);
//                this.settings = builder.Build();
//                string connection = settings.GetConnectionString("Bioreference.LIS");
//                this.reflexSettings = SettingsExtension.Fetch(connection, "B2ReflexesToSPM", "");
//            }
//            catch (Exception ex)
//            {
//                logger.LogError(ex, ex.Message);
//                throw;
//            }
//        }

//        public override ConsumerProcessingResult ProcessMessage(MessageContext<Reflex> messageContext)
//        {
//            try
//            {
//                Reflex msg = messageContext.Message.Payload;
//                logger.LogInformation($"Received Reflex Message '{msg.AccessionNumber}:{msg.OrderedCode}->{msg.ReflexCode}'");
//                string accessionNumber = msg.AccessionNumber.Trim();
//                if (accessionNumber.Length == 7)
//                    accessionNumber = $"10{accessionNumber}";
//                string json = string.Empty;
//                B2AddOnData b2Data = new B2AddOnData();
//                B2AddOnTest testData = new B2AddOnTest()
//                {
//                    TestCode = msg.ReflexCode,
//                    AccessionNumber = accessionNumber,
//                    DateOrdered = msg.DateOrdered,
//                    ActionType = ActionTypeEnum.Add,
//                    Metadata = [new MetaData() { Name = "IsB2ReflexTest", Value = "True" }],
//                    TestSource = "",
//                    TestStatus = "",
//                    TestName = "",
//                    Comments = "",
//                    DateCollected = ""
//                };
//                b2Data.Tests.Add(testData);
//                json = JsonConvert.SerializeObject(b2Data, Newtonsoft.Json.Formatting.Indented);
//                logger.LogDebug($"ETS Payload: {json}");
//                string jsonResponse = string.Empty;
//                WebRequest wr = WebRequest.Create(reflexSettings.GetString("ETSUrl"));
//                wr.ContentType = "application/x-www-form-urlencoded";
//                wr.Method = "POST";
//                string content = $"callingApplication=B2_REFLEXES&accessionNumber={accessionNumber}&payload={json}&un={reflexSettings.GetString("ETSUserID")}&pd={reflexSettings.GetString("ETSPassword")}";
//                Byte[] bytes = System.Text.UTF8Encoding.UTF8.GetBytes(content);
//                wr.ContentLength = bytes.Length;
//                System.IO.Stream s = wr.GetRequestStream();
//                s.Write(bytes, 0, bytes.Length);
//                s.Close();
//                using (WebResponse resp = wr.GetResponse())
//                {
//                    using (StreamReader rdr = new StreamReader(resp.GetResponseStream()))
//                    {
//                        jsonResponse = rdr.ReadToEnd();
//                        logger.LogDebug($"ETS Response: {jsonResponse}");
//                    }
//                    Result result = JsonConvert.DeserializeObject<Result>(jsonResponse);
//                }
//                return ConsumerProcessingResult.Success();
//            }
//            catch (Exception ex)
//            {
//                logger.LogError(ex, ex.Message);
//                return ConsumerProcessingResult.Failure(ex.Message);
//            }
//        }
//    }
//}

