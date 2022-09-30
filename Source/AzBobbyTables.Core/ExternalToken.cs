using Azure.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PipeHow.AzBobbyTables.Core
{
    public class ExternalTokenCredential : TokenCredential
    {
        private string token;
        private DateTimeOffset expiresOn;

        public ExternalTokenCredential(string token, DateTimeOffset expiresOn)
        {
            this.token = token;
            this.expiresOn = expiresOn;
        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new AccessToken(token, expiresOn);
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new ValueTask<AccessToken>(new AccessToken(token, expiresOn));
        }
    }
}
