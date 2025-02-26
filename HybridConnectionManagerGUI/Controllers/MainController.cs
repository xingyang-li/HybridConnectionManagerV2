﻿using HcManProto;
using HybridConnectionManagerGUI.Models;
using HybridConnectionManager.Library;
using Microsoft.AspNetCore.Mvc;

namespace HybridConnectionManagerGUI.Controllers
{
    public class MainController : Controller
    {
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
    }
}
