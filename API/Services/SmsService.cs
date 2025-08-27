using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using API.Helpers;
using API.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace API.Services
{
    public class SmsService : ISmsService
    {
        private readonly HttpClient _httpClient;
        private readonly SmsGatewaySettings _settings;
        private readonly ILogger<SmsService> _logger;

        public SmsService(HttpClient httpClient, IOptions<SmsGatewaySettings> settings, ILogger<SmsService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<bool> SendSmsAsync(string mobileNumber, string message)
        {
            var requestUrl = "https://app.gsoftsolution.com/api/v1/sms/send";

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Add("apikey", _settings.ApiKey);

            var parameters = new Dictionary<string, string>
            {
                { "message", message },
                { "mobile_number", mobileNumber },
                { "device", _settings.DeviceId ?? string.Empty }
            };
            request.Content = new FormUrlEncodedContent(parameters);

            try
            {
                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = JsonDocument.Parse(responseBody);
                    if (jsonResponse.RootElement.TryGetProperty("success", out var successElement) && successElement.GetBoolean())
                    {
                        _logger.LogInformation("SMS sent successfully to {MobileNumber}", mobileNumber);
                        return true;
                    }
                }
                
                _logger.LogError("Failed to send SMS to {MobileNumber}. Status: {StatusCode}, Response: {ResponseBody}", 
                    mobileNumber, response.StatusCode, responseBody);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while sending SMS to {MobileNumber}", mobileNumber);
                return false;
            }
        }
    }
}