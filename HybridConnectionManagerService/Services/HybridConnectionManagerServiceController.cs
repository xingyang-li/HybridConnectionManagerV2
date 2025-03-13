using Azure.Core;
using Grpc.Core;
using HcManProto;
using HybridConnectionManager.Library;
using Serilog;
using System.Text.RegularExpressions;

namespace HybridConnectionManager.Service
{
    public class HybridConnectionManagerServiceController : HcMan.HcManBase
    {
        private static HybridConnectionManager _hybridConnectionManager;

        public HybridConnectionManagerServiceController(){ }

        public static HybridConnectionManager HybridConnectionManager
        {
            get
            {
                if (_hybridConnectionManager == null)
                {
                    _hybridConnectionManager = HybridConnectionManager.Instance;
                }

                return _hybridConnectionManager;
            }
        }

        public override async Task<HybridConnectionInformationResponse> AddUsingConnectionString(HybridConnectionRequest request, ServerCallContext context)
        {
            if (!Regex.IsMatch(request.ConnectionString, Util.HcConnectionStringRegexPattern))
            {
                LogProvider.LogError(String.Format("Tried to add connection with invalid connection string {0}.", request.ConnectionString));
            }

            var connectionStringInfo = Util.GetInformationFromConnectionString(request.ConnectionString);

            LogProvider.LogOperation("ADD", connectionStringInfo.Namespace, connectionStringInfo.Name);

            if (HybridConnectionManager.FindConnectionInformation(request.ConnectionString, out HybridConnectionInformation hcInfo))
            {
                // Allow for overwriting dead connection
                if (hcInfo.Status != "NotFound")
                {
                    LogProvider.LogInfo(string.Format("Connection {0}/{1} already exists", connectionStringInfo.Namespace, connectionStringInfo.Name));

                    return new HybridConnectionInformationResponse
                    {
                        Error = true,
                        ErrorMessage = "Connection already exists."
                    };
                }
            }

            var connectionInformation = await HybridConnectionManager.AddWithConnectionString(request.ConnectionString, request.SubscriptionId, request.ResourceGroup);

            if (connectionInformation != null)
            {
                return new HybridConnectionInformationResponse
                {
                    Namespace = connectionInformation.Namespace,
                    Name = connectionInformation.Name,
                    Endpoint = !String.IsNullOrEmpty(connectionInformation.EndpointHost) ? connectionInformation.EndpointHost + ":" + connectionInformation.EndpointPort: string.Empty,
                    Status = connectionInformation.Status,
                    NumberOfListeners = connectionInformation.NumberOfListeners,
                    ServiceBusEndpoint = connectionInformation.Namespace + ".servicebus.windows.net",
                    CreatedOn = connectionInformation.CreatedOn ?? string.Empty,
                    LastUpdated = connectionInformation.LastUpdated ?? string.Empty,
                };
            }
            else
            {
                return new HybridConnectionInformationResponse
                {
                    Error = true,
                    ErrorMessage = "Could not add connection."
                };
            }
        }

        public override async Task<HybridConnectionInformationResponse> AddUsingParameters(HybridConnectionRequest request, ServerCallContext context)
        {
            LogProvider.LogOperation("ADD", request.Namespace, request.Name);

            if (HybridConnectionManager.FindConnectionInformation(request.Namespace, request.Name, out HybridConnectionInformation hcInfo))
            {
                // Allow for overwriting dead connection
                if (hcInfo.Status != "NotFound")
                {
                    LogProvider.LogInfo(string.Format("Connection {0}/{1} already exists", request.Namespace, request.Name));

                    return new HybridConnectionInformationResponse
                    {
                        Error = true,
                        ErrorMessage = "Connection already exists."
                    };
                }
            }

            if (!HybridConnectionManager.IsValidToken())
            {
                return new HybridConnectionInformationResponse
                {
                    Error = true,
                    ErrorMessage = "AccessToken is not set or expired. Please log in to Azure."
                };
            }

            var connectionInformation = await HybridConnectionManager.AddWithParameters(request.SubscriptionId, request.ResourceGroup, request.Namespace, request.Name);

            if (connectionInformation != null)
            {
                return new HybridConnectionInformationResponse
                {
                    Namespace = connectionInformation.Namespace,
                    Name = connectionInformation.Name,
                    Endpoint = connectionInformation.EndpointHost + ":" + connectionInformation.EndpointPort,
                    Status = connectionInformation.Status,
                    NumberOfListeners = connectionInformation.NumberOfListeners,
                    ServiceBusEndpoint = connectionInformation.Namespace + ".servicebus.windows.net",
                    CreatedOn = connectionInformation.CreatedOn,
                    LastUpdated = connectionInformation.LastUpdated,
                };
            }
            else
            {
                return new HybridConnectionInformationResponse
                {
                    Error = true,
                    ErrorMessage = "Could not add connection."
                };
            }
        }

        public override async Task<StringResponse> RemoveConnection(HybridConnectionRequest request, ServerCallContext context)
        {
            LogProvider.LogOperation("REMOVE", request.Namespace, request.Name);

            StringResponse response = new StringResponse();

            var success = await HybridConnectionManager.RemoveConnection(request.Namespace, request.Name);
            
            if (success)
            {
                LogProvider.LogInfo(string.Format("Removed Hybrid Connection {0}/{1}", request.Namespace, request.Name));
                response.Content = "Connection removed successfully";
            }
            else
            {
                LogProvider.LogInfo(string.Format("Could not remove Hybrid Connection {0}/{1}", request.Namespace, request.Name));
                response.Content = "Could not remove connection.";
            }

            return response;
        }

        public override async Task<HybridConnectionInformationResponse> ShowConnection(HybridConnectionRequest request, ServerCallContext context)
        {
            if (HybridConnectionManager.FindConnectionInformation(request.Namespace, request.Name, out HybridConnectionInformation connectionInformation))
            {
                return new HybridConnectionInformationResponse
                {
                    Namespace = connectionInformation.Namespace,
                    Name = connectionInformation.Name,
                    Endpoint = !String.IsNullOrEmpty(connectionInformation.EndpointHost) ? connectionInformation.EndpointHost + ":" + connectionInformation.EndpointPort : string.Empty,
                    Status = connectionInformation.Status,
                    NumberOfListeners = connectionInformation.NumberOfListeners,
                    ServiceBusEndpoint = connectionInformation.Namespace + ".servicebus.windows.net",
                    CreatedOn = connectionInformation.CreatedOn ?? string.Empty,
                    LastUpdated = connectionInformation.LastUpdated ?? string.Empty,
                };
            }

            return new HybridConnectionInformationResponse
            {
                Error = true,
                ErrorMessage = "Cound not find hybrid connection"
            };
        }

        public override async Task<HybridConnectionInformationListResponse> ListConnections(EmptyRequest request, ServerCallContext context)
        {
            List<HybridConnectionInformationResponse> connectionInformationsResponse = new List<HybridConnectionInformationResponse>();
            
            var connectionInformations = HybridConnectionManager.GetAllConnectionInformations();
            foreach (var connectionInformation in connectionInformations)
            {
                connectionInformationsResponse.Add(new HybridConnectionInformationResponse
                {
                    Namespace = connectionInformation.Namespace,
                    Name = connectionInformation.Name,
                    Endpoint = !String.IsNullOrEmpty(connectionInformation.EndpointHost) ? connectionInformation.EndpointHost + ":" + connectionInformation.EndpointPort : string.Empty,
                    Status = connectionInformation.Status,
                    NumberOfListeners = connectionInformation.NumberOfListeners,
                    ServiceBusEndpoint = connectionInformation.Namespace + ".servicebus.windows.net",
                    CreatedOn = connectionInformation.CreatedOn ?? string.Empty,
                    LastUpdated = connectionInformation.LastUpdated ?? string.Empty,
                    SubscriptionId = connectionInformation.SubscriptionId ?? string.Empty,
                    ResourceGroup = connectionInformation.ResourceGroup ?? string.Empty,
                });
            }

            return new HybridConnectionInformationListResponse { ConnectionInformations = { connectionInformationsResponse } };
        }

        public override async Task<StringResponse> RestartConnection(HybridConnectionRequest request, ServerCallContext context)
        {
            LogProvider.LogOperation("RESTART", request.Namespace, request.Name);

            return await HybridConnectionManager.RestartConnection(request.Namespace, request.Name);
        }

        public override async Task<StringResponse> TestEndpointForConnection(EndpointRequest request, ServerCallContext context)
        {
            if (!Regex.IsMatch(request.Endpoint, Util.EndpointRegexPattern) && !Regex.IsMatch(request.Endpoint, Util.ServiceBusRegexPattern))
            {
                return new StringResponse
                {
                    Error = true,
                    Content = String.Format("Endpoint {0} is of an incorrect format.", request.Endpoint)
                };
            }

            var response = await Util.ConnectToEndpoint(request.Endpoint);

            LogProvider.LogTestEvent(request.Endpoint, response.Content);

            return new StringResponse
            {
                Content = response.Content,
                Error = response.Error
            };
        }

        public override async Task<StringResponse> AuthenticateUser(EmptyRequest request, ServerCallContext context)
        {
            await HybridConnectionManager.AuthenticateToAzure();
            AccessToken accessToken = HybridConnectionManager.GetAuthToken();
            return new StringResponse
            {
                Content = String.Format("Authentication Successful. Token expires {0}", accessToken.ExpiresOn)
            };
        }

        public override async Task<StringResponse> GetLogPath(EmptyRequest request, ServerCallContext context)
        {
            return new StringResponse
            {
                Content =  Util.AppDataLogDir
            };
        }
    }
}
