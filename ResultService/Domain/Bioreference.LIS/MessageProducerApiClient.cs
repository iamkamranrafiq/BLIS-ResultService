using System.Text;
using System.Text.Json;


namespace Bioreference.LIS
{
    public class MessageProducerApiClient
    {
        private static readonly ILog Log = LogManager.GetLogger<MessageProducerApiClient>();
        private static readonly HttpClient _httpClient = new HttpClient();

        public static List<(bool Success, string Response, int StatusCode)> Produce(string MessageType, string Message, string Key)
        {
            var results = new List<(bool, string, int)>();

            try
            {
                Key = StringExtensions.Get9Digit(Key);
                var producers = Configuration.AppSettings.GetSection("Bioreference.LIS:MessageProducer").GetChildren();

                foreach (var producer in producers)
                {
                    string? topicName = producer["Topic"];
                    string? messageType = producer["MessageType"];
                    if (String.Equals(MessageType, messageType, StringComparison.OrdinalIgnoreCase))
                    {
                        Log.Info($"Processing producer. Topic: {topicName}, ChannelSource: {messageType}");

                        var producerHost = Environment.GetEnvironmentVariable("GATEWAY_HOST") ?? string.Empty;
                        var apiUrl = $"{producerHost}/kafka/api/Producer/Produce";

                        Log.Info($"API URL: {apiUrl}");

                        string sourceKey = Configuration.AppSettings.GetString("Bioreference.ConsumerLogging:LoggingName");

                        var payload = new
                        {
                            source = sourceKey,
                            topic = topicName,
                            key = Key,
                            logKey = Key,
                            payload = Message,
                            headers = new[] { new { name = "", value = "" } }
                        };

                        string jsonPayload = JsonSerializer.Serialize(payload);

                        Log.Debug($"Request Payload: {jsonPayload}");

                        var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
                        {
                            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                        };

                        request.Headers.Accept.ParseAdd("text/plain");

                        Log.Info("Sending HTTP request to Kafka Producer API.");

                        var response = _httpClient.Send(request);
                        var responseContent = response.Content.ReadAsStringAsync().Result;

                        Log.Info($"Response received. StatusCode: {(int)response.StatusCode}");
                        Log.Debug($"Response Content: {responseContent}");

                        results.Add((response.IsSuccessStatusCode, responseContent, (int)response.StatusCode));
                    }

                    Log.Info("Produce method completed successfully.");
                }
            }
            catch (Exception ex)
            {
                Log.Error("Exception occurred in Produce method.", ex);
                results.Add((false, ex.Message, 500));
            }

            return results;
        }
    }

}
