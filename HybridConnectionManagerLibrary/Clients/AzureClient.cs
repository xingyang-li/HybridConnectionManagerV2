using Azure.Core;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;

namespace HybridConnectionManager.Library
{
    public class AzureClient
    {
        private HttpClient _httpClient;

        public static string SubscriptionsApiVersion = "2020-01-01";
        public static string AzureRelayApiVersion = "2024-01-01";
        public static string ManagementBaseUrl = "https://management.azure.com";

        private AccessToken _accessToken;
        private object _tokenLock = new object();

        public AzureClient()
        {
            _httpClient = new HttpClient();
            _accessToken = new AccessToken();
        }

        public void SetToken(AccessToken accessToken)
        {
            lock (_tokenLock)
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token);
                _accessToken = accessToken;
            }
        }

        public AccessToken GetToken()
        {
            lock (_tokenLock)
            {
                return _accessToken;
            }
        }

        public async Task<HttpResponseMessage> GetSubscriptions()
        {
            string url = String.Format("{0}/subscriptions?api-version={1}", ManagementBaseUrl, SubscriptionsApiVersion);
            return await GetAsync(url);
        }

        public async Task<AzureListResponseEnvelope<AuthorizationRules>> GetHybridConnectionAuthorizationRules(string subscription, string resourceGroup, string @namespace, string name)
        {
            string url = String.Format("{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.Relay/namespaces/{3}/hybridconnections/{4}/authorizationRules?api-version={5}", 
                ManagementBaseUrl, subscription, resourceGroup, @namespace, name, AzureRelayApiVersion);
            return await GetAsync<AzureListResponseEnvelope<AuthorizationRules>>(url);
        }

        public async Task<AzureListResponseEnvelope<AuthorizationRules>> GetNamespaceAuthorizationRules(string subscription, string resourceGroup, string @namespace)
        {
            string url = String.Format("{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.Relay/namespaces/{3}/authorizationRules?api-version={4}",
                ManagementBaseUrl, subscription, resourceGroup, @namespace, AzureRelayApiVersion);
            return await GetAsync<AzureListResponseEnvelope<AuthorizationRules>>(url);
        }

        public async Task<AuthorizationRuleKeys> GetHybridConnectionAuthorizationRuleKeys(string subscription, string resourceGroup, string @namespace, string name, string ruleName)
        {
            string url = String.Format("{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.Relay/namespaces/{3}/hybridconnections/{4}/authorizationRules/{5}/listKeys?api-version={6}",
                ManagementBaseUrl, subscription, resourceGroup, @namespace, name, ruleName, AzureRelayApiVersion);
            return await PostAsync<AuthorizationRuleKeys>(url);
        }

        public async Task<AuthorizationRuleKeys> GetNamespaceAuthorizationRuleKeys(string subscription, string resourceGroup, string @namespace, string ruleName)
        {
            string url = String.Format("{0}/subscriptions/{1}/resourceGroups/{2}/providers/Microsoft.Relay/namespaces/{3}/authorizationRules/{4}/listKeys?api-version={5}",
                ManagementBaseUrl, subscription, resourceGroup, @namespace, ruleName, AzureRelayApiVersion);
            return await PostAsync<AuthorizationRuleKeys>(url);
        }

        private async Task<HttpResponseMessage> GetAsync(string url)
        {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            var httpResponseMessage = await SendHttpRequestMessage(httpRequestMessage);
            await EnsureSuccessStatusCode(httpResponseMessage);
            return httpResponseMessage;
        }

        private async Task<T> GetAsync<T>(string url)
        {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            var httpResponseMessage = await this.SendHttpRequestMessage(httpRequestMessage);
            await EnsureSuccessStatusCode(httpResponseMessage);

            return await httpResponseMessage.Content.ReadAsAsync<T>();
        }

        private async Task<T> PostAsync<T>(string url)
        {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, url);
            var httpResponseMessage = await this.SendHttpRequestMessage(httpRequestMessage);
            await EnsureSuccessStatusCode(httpResponseMessage);
            return await httpResponseMessage.Content.ReadAsAsync<T>();
        }

        private async Task<HttpResponseMessage> SendHttpRequestMessage(HttpRequestMessage httpRequestMessage)
        {
            var httpResponseMessage = await _httpClient.SendAsync(httpRequestMessage);
            return httpResponseMessage;
        }

        private async Task EnsureSuccessStatusCode(HttpResponseMessage httpResponseMessage)
        {
            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                var httpException = new AzureClientException
                {
                    RequestMethod = httpResponseMessage.RequestMessage.Method.ToString(),
                    RequestUri = httpResponseMessage.RequestMessage.RequestUri.AbsoluteUri,
                    StatusCode = httpResponseMessage.StatusCode,
                };

                httpException.Content = "<<< exception while reading content >>>";
                httpException.ErrorCode = "<<< exception while reading error code >>>";

                try
                {
                    var contentString = await httpResponseMessage.Content.ReadAsStringAsync();
                    var contentStringTruncated = contentString.Substring(0, Math.Min(1024, contentString.Length));
                    httpException.Content = contentStringTruncated;

                    var errorEntity = JsonConvert.DeserializeObject<AzureErrorEntity>(contentString);
                    httpException.ErrorCode = errorEntity.Error.Code;
                }
                catch
                {
                }

                throw httpException;
            }
        }
    }

    public class AzureClientException : Exception
    {
        public override string Message
        {
            get
            {
                return string.Format("For request {0} {1}, received a response with status code {2}, error code {3}, and response content: {4}. Request content was {5}",
                                     this.RequestMethod,
                                     this.RequestUri,
                                     this.StatusCode,
                                     this.ErrorCode,
                                     this.Content,
                                     this.RequestContent);
            }
        }

        public string RequestMethod { get; set; }
        public string RequestUri { get; set; }
        public string RequestContent { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string ErrorCode { get; set; }
        public string Content { get; set; }
    }

    public class AzureErrorEntity
    {
        public AzureErrorEntityInner Error { get; set; }
    }

    public class AzureErrorEntityInner
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }
}
