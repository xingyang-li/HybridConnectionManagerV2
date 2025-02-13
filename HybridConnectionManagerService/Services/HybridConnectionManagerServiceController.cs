using Azure.Core;
using Azure.Identity;
using Grpc.Core;
using HcManProto;

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

        public override async Task<StringResponse> AddUsingConnectionString(AddConnectionRequest request, ServerCallContext context)
        {
            // Get info from Azure using connectionstring
            // addd hcinfo to map
            // start listening using hc
            await HybridConnectionManager.AddConnectionWithConnectionStringAsync(request.ConnectionString);

            var hcInfo = Util.GetInformationFromConnectionString(request.ConnectionString);
            var connection = HybridConnectionManager.FindConnection(hcInfo.Namespace, hcInfo.Name);

            StringResponse response = new StringResponse();

            if (connection != null)
            {
                response.Content = String.Format("Connection {0} in namespace {1} listening to {2}:{3}", connection.Information.Name, connection.Information.Namespace, connection.Information.EndpointHost, connection.Information.EndpointPort);
            }
            else
            {
                response.Content = "Failed to add connection.";
            }


            return response;
        }

        public override async Task<StringResponse> RemoveConnection(RemoveConnectionRequest request, ServerCallContext context)
        {
            await HybridConnectionManager.RemoveConnection(request.Namespace, request.Name);
            return new StringResponse
            {
                Content = String.Format("Connection {0} in namespace {1} removed", request.Name, request.Namespace)
            };
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
