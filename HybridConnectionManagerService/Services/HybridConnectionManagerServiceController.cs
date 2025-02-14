using Azure.Core;
using Azure.Identity;
using Grpc.Core;
using HcManProto;
using HybridConnectionManager.Models;
using Newtonsoft.Json;

namespace HybridConnectionManager.Service
{
    public class HybridConnectionManagerServiceController : HcMan.HcManBase
    {
        private readonly ILogger<HybridConnectionManagerServiceController> _logger;

        private static HybridConnectionManager _hybridConnectionManager;

        public HybridConnectionManagerServiceController(ILogger<HybridConnectionManagerServiceController> logger)
        {
            _logger = logger;
        }

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
            if (HybridConnectionManager.FindConnectionInformation(request.ConnectionString, out HybridConnectionInformation _))
            {
                return new HybridConnectionInformationResponse
                {
                    Error = true,
                    ErrorMessage = "Connection already exists."
                };
            }

            var connectionInformation = await HybridConnectionManager.AddWithConnectionString(request.ConnectionString);

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
            StringResponse response = new StringResponse();

            var success = await HybridConnectionManager.RemoveConnection(request.Namespace, request.Name);
            
            if (success)
            {
                response.Content = "Connection removed Successfully";
            }
            else
            {
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
                    Endpoint = connectionInformation.EndpointHost + ":" + connectionInformation.EndpointPort,
                    Status = connectionInformation.Status,
                    NumberOfListeners = connectionInformation.NumberOfListeners,
                    ServiceBusEndpoint = connectionInformation.Namespace + ".servicebus.windows.net",
                };
            }

            return new HybridConnectionInformationResponse
            {
                Error = true,
                ErrorMessage = "Cound not find hybrid connection"
            };
        }

        public override async Task<StringResponse> TestEndpointForConnection(EndpointRequest request, ServerCallContext context)
        {
            string responseStr = await Util.ConnectToEndpoint(request.Endpoint);
            return new StringResponse { Content = responseStr };
        }

        public override async Task<StringResponse> AuthenticateUser(AuthRequest request, ServerCallContext context)
        {
            await HybridConnectionManager.AuthenticateToAzure();
            AccessToken accessToken = HybridConnectionManager.GetAuthToken();
            return new StringResponse
            {
                Content = String.Format("Authentication Successful. Token expires {0}", accessToken.ExpiresOn)
            };
        }
    }
}
