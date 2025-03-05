using HcManProto;
using HybridConnectionManagerGUI.Models;
using HybridConnectionManagerGUI.Services;
using HybridConnectionManager.Library;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Relay;
using System.Net.Sockets;

namespace HybridConnectionManagerGUI.Controllers
{
    public class MainController : Controller
    {
        private RelayArmClient _client;

        private TcpConnectionChecker _connectionChecker;

        public MainController(TcpConnectionChecker connectionChecker)
        {
            _connectionChecker = connectionChecker;
        }

        public IActionResult Index()
        {
            var hybridConnections = GetHybridConnections();
            return View(hybridConnections);
        }

        public IActionResult GetUpdatedData()
        {
            var hybridConnections = GetHybridConnections();
            return PartialView("_ItemsPartial", hybridConnections); // Return a partial view with just the table contents
        }

        [HttpPost]
        public JsonResult Remove([FromBody] HybridConnectionsModel model)
        {
            try
            {
                RemoveHybridConnections(model);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult Add([FromBody] HybridConnectionModel model)
        {
            if (!Regex.IsMatch(model.ConnectionString, Util.HcConnectionStringRegexPattern))
            {
                return Json(new { success = false, Message = "Please use a valid connection string." });
            }
            return AddHybridConnection(model);
        }

        [HttpPost]
        public JsonResult AddMultiple([FromBody] HybridConnectionsModel model)
        {
            foreach (var connection in model.Connections)
            {
                if (!Regex.IsMatch(connection.ConnectionString, Util.HcConnectionStringRegexPattern))
                {
                    return Json(new { success = false, Message = "Please use a valid connection string." });
                }
                AddHybridConnection(connection);
            }
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult Test([FromQuery] string endpoint)
        {
            if (!Regex.IsMatch(endpoint, Util.EndpointRegexPattern))
            {
                return Json(new { success = false, Message = "Please use a valid endpoint of form [host]:[port]." });
            }

            try
            {
                HybridConnectionManagerClient client = new HybridConnectionManagerClient();
                var response = client.TestEndpointForConnection(endpoint).Result;
                return Json(new { success = !response.Error, Message = response.Content });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, Message = "Hybrid Connection Manager Service not reachable. Please make sure the service is running. " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckTcpConnection()
        {
            try
            {
                bool isConnected = await _connectionChecker.CheckConnectionAsync("localhost", 5001);

                return Json(new
                {
                    isConnected = isConnected,
                    checkedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    isConnected = false,
                    error = ex.Message,
                    checkedAt = DateTime.UtcNow
                });
            }
        }

        public List<HybridConnectionModel> GetHybridConnectionsForSubscription(string subscriptionId)
        {
            List<HybridConnectionModel> hybridConnectionsList = new List<HybridConnectionModel>();
            var credential = MSALProvider.TokenCredential;
            _client = new RelayArmClient(credential);
            var subscriptionResource = _client.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"));
            var hybridConnections = subscriptionResource.GetRelayNamespaces().SelectMany(ns => ns.GetRelayHybridConnections());
            foreach (var hybridConnection in hybridConnections)
            {
                if (!String.IsNullOrEmpty(hybridConnection.Data.UserMetadata))
                {
                    string endpoint = Util.GetEndpointStringFromUserMetadata(hybridConnection.Data.UserMetadata);
                    if (!String.IsNullOrEmpty(endpoint))
                    {
                        string connectionString = _client.GetHybridConnectionPrimaryConnectionString(hybridConnection.Data.Id);
                        var connection = new HybridConnectionModel
                        {
                            Namespace = hybridConnection.Data.Name,
                            Name = hybridConnection.Data.Name,
                            Endpoint = endpoint,
                            ConnectionString = connectionString
                        };

                        hybridConnectionsList.Add(connection);
                    }
                }
            }

            return hybridConnectionsList;
        }

        public List<Subscription> GetSubscriptions()
        {
            List<Subscription> subscriptions = new List<Subscription>();
            var credential = MSALProvider.TokenCredential;
            _client = new RelayArmClient(credential);
            var subscriptionsCollection = _client.GetSubscriptions();
            foreach (var subscriptionResource in subscriptionsCollection)
            {
                subscriptions.Add(new Subscription { DisplayName = subscriptionResource.Data.DisplayName, SubscriptionId = subscriptionResource.Data.SubscriptionId});
            }

            return subscriptions;
        }

        public List<HybridConnectionModel> GetHybridConnections()
        {
            HybridConnectionManagerClient client = new HybridConnectionManagerClient();
            List<HybridConnectionModel> hybridConnections = new List<HybridConnectionModel>();
            try
            {
                var hybridConnectionsResponse = client.ListConnections().Result;
                foreach (var hybridConnection in hybridConnectionsResponse)
                {
                    hybridConnections.Add(new HybridConnectionModel
                    {
                        Namespace = hybridConnection.Namespace,
                        Name = hybridConnection.Name,
                        Endpoint = hybridConnection.Endpoint,
                        Status = hybridConnection.Status,
                        CreatedOn = hybridConnection.CreatedOn,
                        LastUpdated = hybridConnection.LastUpdated,
                    });
                }

                return hybridConnections;
            }
            catch (Exception ex)
            {
                return new List<HybridConnectionModel>();
            }
        }

        public void RemoveHybridConnections(HybridConnectionsModel model)
        {
            HybridConnectionManagerClient client = new HybridConnectionManagerClient();

            foreach (var connection in model.Connections)
            {
                var hybridConnectionsResponse = client.RemoveConnection(connection.Namespace, connection.Name).Result;
            }
        }

        public JsonResult AddHybridConnection(HybridConnectionModel model)
        {
            try
            {
                HybridConnectionManagerClient client = new HybridConnectionManagerClient();
                var response = client.AddUsingConnectionString(model.ConnectionString).Result;

                if (response.Error)
                {
                    return Json(new { success = false, message = response.ErrorMessage });
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Could not connect to Hybrid Connection Manager Service." });
            }
        }

        public List<string> GetLogFiles()
        {
            if (!Directory.Exists(Util.AppDataLogDir)) { return new List<string>(); }
            return Directory.GetFiles(Util.AppDataLogDir).ToList();
        }

        public string GetLogContent(string fileName)
        {
            using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            return reader.ReadToEndAsync().Result;
        }
    }
}
