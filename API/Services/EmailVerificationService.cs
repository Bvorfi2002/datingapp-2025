using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Web;
using API.DTOs;
using API.Interfaces;
using Microsoft.Extensions.Configuration;

namespace API.Services
{
    public class EmailVerificationService : IEmailVerificationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public EmailVerificationService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["API_Email_Verifier"] ?? throw new Exception("Email verifier API key not found");
        }
        public async Task<bool> IsEmailValidAsync(string email)
        {
            var apiURL = $"https://api.quickemailverification.com/v1/verify?email={HttpUtility.UrlEncode(email)}&apikey={_apiKey}";

            try
            {
                var verificationResult = await _httpClient.GetFromJsonAsync<EmailVerificationDto>(apiURL);
                return verificationResult?.Result == "valid";
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}