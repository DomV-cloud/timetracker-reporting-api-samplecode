using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using TimetrackerReportingClient.Fakturoid.Models;

namespace TimetrackerReportingClient.Fakturoid
{
    /// <summary>
    /// Client for the Fakturoid API v3.
    /// Uses OAuth2 Client Credentials flow.
    ///
    /// Credentials are obtained from: Settings → User account → OAuth 2 credentials
    /// Slug is visible in the URL when logged in: app.fakturoid.cz/api/v3/accounts/{slug}/
    /// </summary>
    public class FakturoidClient
    {
        private const string BaseUrl    = "https://app.fakturoid.cz/api/v3";
        private const string TokenUrl   = "https://app.fakturoid.cz/api/v3/oauth/token";
        private const string AppName    = "TimePaceInvoicer (pbarabas@example.com)";

        private readonly string _slug;
        private readonly string _clientId;
        private readonly string _clientSecret;

        private string _accessToken;

        public FakturoidClient(string slug, string clientId, string clientSecret)
        {
            _slug         = slug;
            _clientId     = clientId;
            _clientSecret = clientSecret;
        }

        /// <summary>
        /// Creates an invoice in Fakturoid and returns the new invoice URL.
        /// </summary>
        public string CreateInvoice(InvoiceRequest invoice)
        {
            EnsureAccessToken();

            var client = new RestClient(BaseUrl);
            var request = new RestRequest($"accounts/{_slug}/invoices.json", Method.POST);

            request.AddHeader("Authorization", "Bearer " + _accessToken);
            request.AddHeader("User-Agent", AppName);
            request.AddHeader("Content-Type", "application/json");

            var body = JsonConvert.SerializeObject(invoice);
            request.AddParameter("application/json", body, ParameterType.RequestBody);

            var response = client.Execute(request);

            if (!response.IsSuccessful)
            {
                throw new Exception(
                    $"Fakturoid invoice creation failed [{response.StatusCode}]: {response.Content}");
            }

            // Extract the Location header (URL of the created invoice)
            var location = response.Headers
                .ToList()
                .Find(h => h.Name?.Equals("Location", StringComparison.OrdinalIgnoreCase) == true)
                ?.Value?.ToString();

            // Also extract the invoice number from the response body
            var responseJson = JObject.Parse(response.Content);
            var invoiceNumber = responseJson["number"]?.ToString();

            Console.WriteLine($"  Invoice number: {invoiceNumber}");
            return location ?? $"{BaseUrl}/accounts/{_slug}/invoices.json";
        }

        private void EnsureAccessToken()
        {
            if (!string.IsNullOrEmpty(_accessToken))
                return;

            var client = new RestClient(TokenUrl);
            var request = new RestRequest(Method.POST);

            // Basic Auth: Base64(client_id:client_secret)
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));

            request.AddHeader("Authorization", "Basic " + credentials);
            request.AddHeader("User-Agent", AppName);
            request.AddHeader("Content-Type", "application/json");
            request.AddParameter("application/json",
                "{\"grant_type\":\"client_credentials\"}",
                ParameterType.RequestBody);

            var response = client.Execute(request);

            if (!response.IsSuccessful)
            {
                throw new Exception(
                    $"Fakturoid authentication failed [{response.StatusCode}]: {response.Content}");
            }

            var token = JsonConvert.DeserializeObject<TokenResponse>(response.Content);
            _accessToken = token?.AccessToken
                ?? throw new Exception("Fakturoid returned an empty access token.");
        }
    }
}
