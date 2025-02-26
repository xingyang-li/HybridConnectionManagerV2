using HcManProto;
using HybridConnectionManagerGUI.Models;
using HybridConnectionManager.Library;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using Azure.Core;
using Azure.ResourceManager.Relay;

namespace HybridConnectionManagerGUI.Controllers
{
    public class MainController : Controller
    {
        private RelayArmClient _client;
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
                Console.WriteLine(connection.ConnectionString);
                if (!Regex.IsMatch(connection.ConnectionString, Util.HcConnectionStringRegexPattern))
                {
                    return Json(new { success = false, Message = "Please use a valid connection string." });
                }
                AddHybridConnection(connection);
            }
            return Json(new { success = true });
        }

        public List<HybridConnectionModel> GetHybridConnectionsForSubscription(string subscriptionId)
        {
            List<HybridConnectionModel> hybridConnectionsList = new List<HybridConnectionModel>();
            var credential = MSALProvider.TokenCredential;
            _client = new RelayArmClient(credential);
            var subscriptionResource = _client.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"));
            var hybridConnections = subscriptionResource.GetResourceGroups().SelectMany(rg => rg.GetRelayNamespaces().SelectMany(ns => ns.GetRelayHybridConnections()));
            Console.WriteLine(hybridConnections.Count());
            foreach (var hybridConnection in hybridConnections)
            {
                string endpoint = Util.GetEndpointStringFromUserMetadata(hybridConnection.Data.UserMetadata);
                string connectionString = _client.GetHybridConnectionPrimaryConnectionString(hybridConnection.Data.Id);
                var connection = new HybridConnectionModel
                {
                    Namespace = hybridConnection.Data.Name,
                    Name = hybridConnection.Data.Name,
                    Endpoint = endpoint,
                    ConnectionString = connectionString,
                };

                Console.WriteLine(hybridConnection.Data.Name);

                hybridConnectionsList.Add(connection);
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
            HybridConnectionManagerClient client = new HybridConnectionManagerClient();
            var response = client.AddUsingConnectionString(model.ConnectionString).Result;

            if (response.Error)
            {
                return Json( new { success = false, message = response.ErrorMessage });
            }

            return Json(new { success = true });
        }

        public List<string> GetLogFiles()
        {
            if (!Directory.Exists(Util.AppDataLogDir)) {  return new List<string>(); }
            return Directory.GetFiles(Util.AppDataLogDir).ToList();
        }
    }
}
