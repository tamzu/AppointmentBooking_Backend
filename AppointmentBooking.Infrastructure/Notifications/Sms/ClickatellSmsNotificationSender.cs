
using AppointmentBooking.Core.Interfaces.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AppointmentBooking.Infrastructure.Notifications.Sms
{
    public class ClickatellSmsNotificationSender
        : ISmsNotificationSender
    {
        private readonly HttpClient _httpClient;
        private readonly ClickatellOptions _options;
        private readonly ILogger<ClickatellSmsNotificationSender> _logger;

        public ClickatellSmsNotificationSender(
            HttpClient httpClient,
            IOptions<ClickatellOptions> options,
            ILogger<ClickatellSmsNotificationSender> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string?> SendAsync(
            string recipient,
            string message,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(recipient))
                throw new ArgumentException("SMS recipient is required.");

            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("SMS message is required.");

            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                throw new InvalidOperationException(
                    "Clickatell API key is not configured.");

            if (!Uri.TryCreate(
                _options.BaseUrl,
                UriKind.Absolute,
                out var baseUri) ||
                (baseUri.Scheme != Uri.UriSchemeHttps &&
                 baseUri.Scheme != Uri.UriSchemeHttp))
            {
                throw new InvalidOperationException(
                    "Clickatell BaseUrl must be a valid absolute HTTP/HTTPS URL.");
            }

            var requestBody = new ClickatellSendRequest
            {
                Content = message,
                To = [recipient],
                From = _options.SenderId
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_options.BaseUrl.TrimEnd('/')}/messages");

            request.Headers.TryAddWithoutValidation(
                "Authorization",
                _options.ApiKey);

            request.Content = JsonContent.Create(requestBody);

            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Clickatell SMS failed. HTTP Status: {StatusCode}. Response: {Response}",
                    (int)response.StatusCode,
                    responseContent);

                throw new HttpRequestException(
                    $"Clickatell returned HTTP {(int)response.StatusCode}: {responseContent}");
            }

            var result = JsonSerializer.Deserialize<ClickatellSendResponse>(
                responseContent,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (result?.Messages == null || result.Messages.Count == 0)
            {
                throw new InvalidOperationException(
                    "Clickatell returned no message delivery result.");
            }

            var rejectedMessage = result.Messages
                .FirstOrDefault(x => !x.Accepted);

            if (rejectedMessage != null)
            {
                throw new InvalidOperationException(
                    $"Clickatell rejected the SMS. Error: {rejectedMessage.ErrorDescription ?? rejectedMessage.Error ?? "Unknown error"}");
            }

            var acceptedMessage = result.Messages
                .First(x => x.Accepted);

            var providerReference = acceptedMessage.ApiMessageId;

            _logger.LogInformation(
                "Clickatell accepted SMS. Provider reference: {ProviderReference}",
                providerReference ?? "Not provided");

            return providerReference;
        }

        private sealed class ClickatellSendRequest
        {
            [JsonPropertyName("content")]
            public string Content { get; set; } = string.Empty;

            [JsonPropertyName("to")]
            public string[] To { get; set; } = [];

            [JsonPropertyName("from")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? From { get; set; }
        }

        private sealed class ClickatellSendResponse
        {
            [JsonPropertyName("messages")]
            public List<ClickatellMessageResult>? Messages { get; set; }

            [JsonPropertyName("errorCode")]
            public int? ErrorCode { get; set; }

            [JsonPropertyName("error")]
            public string? Error { get; set; }

            [JsonPropertyName("errorDescription")]
            public string? ErrorDescription { get; set; }
        }

        private sealed class ClickatellMessageResult
        {
            [JsonPropertyName("apiMessageId")]
            public string? ApiMessageId { get; set; }

            [JsonPropertyName("accepted")]
            public bool Accepted { get; set; }

            [JsonPropertyName("to")]
            public string? To { get; set; }

            [JsonPropertyName("errorCode")]
            public int? ErrorCode { get; set; }

            [JsonPropertyName("error")]
            public string? Error { get; set; }

            [JsonPropertyName("errorDescription")]
            public string? ErrorDescription { get; set; }
        }
    }
}
