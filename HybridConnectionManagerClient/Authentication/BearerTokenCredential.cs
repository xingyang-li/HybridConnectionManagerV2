using Azure.Core;

namespace HybridConnectionManager.Library
{
    public class BearerTokenCredential : TokenCredential
    {
        private readonly string _token;

        public BearerTokenCredential(string token)
        {
            _token = token ?? throw new ArgumentNullException(nameof(token));
        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new AccessToken(_token, DateTimeOffset.MaxValue);
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new ValueTask<AccessToken>(new AccessToken(_token, DateTimeOffset.MaxValue));
        }
    }
}
