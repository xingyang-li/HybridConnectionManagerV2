using Grpc.Core;
using HcManService;

namespace HybridConnectionManager.Service
{
    public class HybridConnectionManagerService : HcMan.HcManBase
    {
        private readonly ILogger<HybridConnectionManagerService> _logger;
        public HybridConnectionManagerService(ILogger<HybridConnectionManagerService> logger)
        {
            _logger = logger;
        }

        public override Task<CommandResponse> AddUsingConnectionString(AddConnectionRequest request, ServerCallContext context)
        {
            return Task.FromResult(new CommandResponse
            {
                Content = "Hello World!"
            });
        }
    }
}
