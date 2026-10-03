using Bioreference.ResultService.Abstractions.Application.ApiClient;
using Bioreference.ResultService.Application.Model.AppSettings.ApiClient;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace Bioreference.ResultService.Application.ApiClient
{
    public class PayloadSenderApiClient : IPayloadSenderApiClient
    {
        private static readonly TimeSpan[] RetryDelays =
        [
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(5)
        ];

        private readonly ILogger<PayloadSenderApiClient> _logger;
        private readonly AppSettingsPayloadSender _appSettings;

        public PayloadSenderApiClient(ILogger<PayloadSenderApiClient> logger, IOptions<AppSettingsPayloadSender> options)
        {
            _logger = logger;
            _appSettings = options.Value;
        }
        public async Task<bool> SendPayloadAsync(string payload, string destination, CancellationToken cancellationToken = default)
        {
            var apiUrl = _appSettings
                ?.SenderUrlMappings
                ?.FirstOrDefault(d => string.Equals(d.Destination, destination, StringComparison.CurrentCultureIgnoreCase))
                ?.Url;

            if (string.IsNullOrEmpty(apiUrl))
            {
                _logger.LogWarning("SenderApiUrl is not configured for {Destination}.", destination);
                return false;
            }

            return await PostPayloadAsync(payload, _appSettings, apiUrl, cancellationToken);
        }

        private async Task<bool> PostPayloadAsync(string payload, AppSettingsPayloadSender senderSettings, string apiUrl, CancellationToken cancellationToken)
        {
            var timeout = TimeSpan.FromSeconds(senderSettings.DestinationEndPointTimeOutSec);
            var maxRetries = RetryDelays.Length;
            var totalAttempts = maxRetries + 1;

            for (var attempt = 1; attempt <= totalAttempts; attempt++)
            {
                try
                {
                    var httpStatus = senderSettings.IgnoreHttps
                        ? await PostWithoutHttpsAsync(payload, apiUrl, timeout)
                        : await PostWithHttpsAsync(payload, apiUrl, timeout);

                    if (httpStatus == (int)HttpStatusCode.OK)
                    {
                        _logger.LogInformation("Payload sent successfully on attempt {Attempt} (retry {RetryCount} of {MaxRetries}). Url: {Url}",
                            attempt, attempt - 1, maxRetries, apiUrl);
                        return true;
                    }

                    if (!IsRetryableStatusCode(httpStatus))
                    {
                        _logger.LogError("Payload send failed with non-retryable status code {StatusCode} on attempt {Attempt}. Url: {Url}",
                            httpStatus, attempt, apiUrl);
                        return false;
                    }

                    if (attempt > maxRetries)
                    {
                        _logger.LogError("Payload send failed after {TotalAttempts} attempts ({MaxRetries} retries). Last status code: {StatusCode}. Url: {Url}",
                            totalAttempts, maxRetries, httpStatus, apiUrl);
                        return false;
                    }

                    var delay = RetryDelays[attempt - 1];
                    _logger.LogWarning("Payload send failed on attempt {Attempt} with status code {StatusCode}. Retry {RetryAttempt} of {MaxRetries} in {DelaySeconds}s. Url: {Url}",
                        attempt, httpStatus, attempt, maxRetries, delay.TotalSeconds, apiUrl);

                    await Task.Delay(delay, cancellationToken);
                }
                catch (FlurlHttpTimeoutException ex)
                {
                    if (attempt > maxRetries)
                    {
                        _logger.LogError(ex, "Payload send failed after {TotalAttempts} attempts ({MaxRetries} retries) due to timeout. Url: {Url}",
                            totalAttempts, maxRetries, apiUrl);
                        return false;
                    }

                    var delay = RetryDelays[attempt - 1];
                    _logger.LogWarning(ex, "Payload send timeout on attempt {Attempt}. Retry {RetryAttempt} of {MaxRetries} in {DelaySeconds}s. Url: {Url}",
                        attempt, attempt, maxRetries, delay.TotalSeconds, apiUrl);

                    await Task.Delay(delay, cancellationToken);
                }
                catch (FlurlHttpException ex)
                {
                    var statusCode = ex.Call?.Response?.StatusCode;

                    if (statusCode.HasValue && IsRetryableStatusCode(statusCode.Value) && attempt <= maxRetries)
                    {
                        var delay = RetryDelays[attempt - 1];
                        _logger.LogWarning(ex, "Payload send failed on attempt {Attempt} with transient Flurl status {StatusCode}. Retry {RetryAttempt} of {MaxRetries} in {DelaySeconds}s. Url: {Url}",
                            attempt, statusCode.Value, attempt, maxRetries, delay.TotalSeconds, apiUrl);

                        await Task.Delay(delay, cancellationToken);
                        continue;
                    }

                    _logger.LogError(ex, "Payload send failed after {Attempt} attempt(s). Flurl error status: {StatusCode}. Url: {Url}",
                        attempt, statusCode, apiUrl);
                    return false;
                }
                catch (HttpRequestException ex)
                {
                    if (attempt > maxRetries)
                    {
                        _logger.LogError(ex, "Payload send failed after {TotalAttempts} attempts ({MaxRetries} retries) due to network error. Url: {Url}",
                            totalAttempts, maxRetries, apiUrl);
                        return false;
                    }

                    var delay = RetryDelays[attempt - 1];
                    _logger.LogWarning(ex, "Payload send network error on attempt {Attempt}. Retry {RetryAttempt} of {MaxRetries} in {DelaySeconds}s. Url: {Url}",
                        attempt, attempt, maxRetries, delay.TotalSeconds, apiUrl);

                    await Task.Delay(delay, cancellationToken);
                }
            }

            return false;
        }

        private async Task<int> PostWithHttpsAsync(string payload, string apiUrl, TimeSpan timeout)
        {
            var response = await GeneratedExtensions.WithSettings(apiUrl, s =>
                {
                    s.Timeout = timeout;
                })
                .AllowAnyHttpStatus()
                .PostStringAsync(payload);

            return response.StatusCode;
        }

        private async Task<int> PostWithoutHttpsAsync(string payload, string apiUrl, TimeSpan timeout)
        {
            var httpClientHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };

            using var flurlClient = new FlurlClient(new HttpClient(httpClientHandler));

            var response = await flurlClient
                .Request(apiUrl)
                .WithTimeout(timeout)
                .AllowAnyHttpStatus()
                .PostStringAsync(payload);

            return response.StatusCode;
        }

        private static bool IsRetryableStatusCode(int statusCode)
        {
            return statusCode == (int)HttpStatusCode.RequestTimeout
                || statusCode == (int)HttpStatusCode.TooManyRequests
                || statusCode == (int)HttpStatusCode.InternalServerError
                || statusCode == (int)HttpStatusCode.BadGateway
                || statusCode == (int)HttpStatusCode.ServiceUnavailable
                || statusCode == (int)HttpStatusCode.GatewayTimeout
                || statusCode == (int)HttpStatusCode.NotFound;
        }
    }
}
