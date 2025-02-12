using Azure.Core;
using Azure.Identity;
using Grpc.Core;
using HcManProto;

namespace HybridConnectionManagerService
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

        public override Task<StringResponse> AddUsingConnectionString(AddConnectionRequest request, ServerCallContext context)
        {
            return Task.FromResult(new StringResponse
            {
                Content = "Hello World!"
            });
        }

        public override async Task<StringResponse> AuthenticateUser(AuthRequest request, ServerCallContext context)
        {
            await Task.Run(() => HybridConnectionManager.AuthenticateToAzure());
            AccessToken accessToken = HybridConnectionManager.GetAuthToken();
            return new StringResponse
            {
                Content = String.Format("Authentication Successful. Token expires {0}", accessToken.ExpiresOn)
            };
        }
    }
}
