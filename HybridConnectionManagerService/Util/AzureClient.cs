using Azure.Core;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;

namespace HybridConnectionManagerService
{
    public class AzureClient
    {
        private HttpClient _httpClient;

        private static string _managementApiVersion = "2020-01-01";
        private static string _managementBaseUrl = "https://management.azure.com";
        private static string _managementSubscriptionsUrl = $"{_managementBaseUrl}/subscriptions?api-version={_managementApiVersion}";

        private AccessToken _accessToken;
        public AzureClient()
        {
            _httpClient = new HttpClient();
            _accessToken = new AccessToken();
        }

        public void SetToken(AccessToken accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);
            _accessToken = accessToken;
        }

        public AccessToken GetToken()
        {
            return _accessToken;
        }

        public async Task<HttpResponseMessage> GetSubscriptions()
        {
            return await GetAsync(_managementSubscriptionsUrl);
        }

        private async Task<HttpResponseMessage> GetAsync(string url)
        {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            return await SendHttpRequestMessage(httpRequestMessage);
        }

        private async Task<T> GetAsync<T>(string url)
        {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            var httpResponseMessage = await this.SendHttpRequestMessage(httpRequestMessage);
            return await httpResponseMessage.Content.ReadAsAsync<T>();
        }

        private async Task<HttpResponseMessage> SendHttpRequestMessage(HttpRequestMessage httpRequestMessage)
        {
            var httpResponseMessage = await _httpClient.SendAsync(httpRequestMessage);
            return httpResponseMessage;
        }
    }
}
