using Bioreference.Contracts.Result;
using Bioreference.ResultService.Abstractions.Application.Processor;
using Bioreference.ResultService.Abstractions.Application.Setting;
using Bioreference.ResultService.Application.Model.AppSettings.CreateManifest;
using Bioreference.ResultService.Common.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using System.Net;
using System.Text;
//using Bioreference.DI.Interface;
//using Bioreference.Messaging.Kafka;
//using Newtonsoft.Json;

namespace Bioreference.ResultService.Application.Processor
{
    public class CreateManifestProcessor : ICreateManifestProcessor
    {
        private ILogger<CreateManifestProcessor> logger;
        private readonly ISettingService settingsProvider;
        private readonly AppSettingsCreateManifest appSettings;
        private CreateManifestSettings dbSettings;

        public CreateManifestProcessor(ILogger<CreateManifestProcessor> logger, ISettingService settingsProvider, IOptions<AppSettingsCreateManifest> options)
        {
            this.logger = logger;
            this.settingsProvider = settingsProvider;
            this.appSettings = options.Value;
        }
        public async Task OnCreateManifestMessage(CreateManifest msg)
        {
            try
            {
                ActivityHelper.SetAccessionLogKey(msg.AccessionNumber);

                logger.LogInformation(
                    "BLIS_ACCESSION_TRACE | AccessionNbr: {AccessionNbr} | Status: Processed by BLIS | Timestamp: {Timestamp}",
                    msg.AccessionNumber,
                    DateTime.UtcNow
                );

                string connection = settingsProvider.GetConnectionString("Bioreference.LIS");

                dbSettings = await settingsProvider.FetchSetting<CreateManifestSettings>(
                    connection,
                    "B2ReflexesToSPM",
                    ""
                );

                string accessionNumber = msg.AccessionNumber.Trim();

                string userName = "b2reflexestospm";
                string callingApp = "b2";
                string clientId = "\"\"";

                string requestUrl =
                    $"{dbSettings.BlisCreateManifestWithDOS}" +
                    $"?clientID={Uri.EscapeDataString(clientId)}" +
                    $"&userName={Uri.EscapeDataString(userName)}" +
                    $"&callingApp={Uri.EscapeDataString(callingApp)}";

                logger.LogDebug(
                    "Entity: {Entity}; Event: {Event}; AccessionNumber: {AccessionNumber}; RequestUrl: {RequestUrl}",
                    "CreateManifest",
                    "BLISPayload",
                    accessionNumber,
                    requestUrl
                );

                using HttpClient client = new HttpClient();

                var request = new
                {
                    AccessionNumbers = new List<long> { long.Parse(accessionNumber) },
                    DateOfService = ""
                };

                using StringContent content = new StringContent(
                    JsonConvert.SerializeObject(request),
                    Encoding.UTF8,
                    "application/json-patch+json"
                );

                using HttpResponseMessage response = await client.PostAsync(requestUrl, content);

                string responseContent = await response.Content.ReadAsStringAsync();

                logger.LogInformation(
                    "Entity: {Entity}; Event: {Event}; AccessionNumber: {AccessionNumber}; StatusCode: {StatusCode}; ContentLength: {ContentLength}",
                    "CreateManifest",
                    "BLISResponse",
                    accessionNumber,
                    response.StatusCode,
                    responseContent?.Length ?? 0
                );

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(
                        $"Error From BLIS Service: Status='{response.StatusCode}', Content='{responseContent}'"
                    );
                }
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Entity: {Entity}; Event: {Event}; AccessionNumber: {AccessionNumber}; Message: {Message}",
                    "CreateManifest",
                    "Error",
                    msg?.AccessionNumber,
                    "Error while processing CreateManifest message."
                );

                throw;
            }
        }
    }
}
