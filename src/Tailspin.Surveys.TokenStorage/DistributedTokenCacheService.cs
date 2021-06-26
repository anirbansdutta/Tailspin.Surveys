using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Tailspin.Surveys.Common;

namespace Tailspin.Surveys.TokenStorage
{
    public class DistributedTokenCacheService : TokenCacheService
    {
        private IDataProtectionProvider _dataProtectionProvider;
        private IDistributedCache _distributedCache;

        /// <summary>
        /// Initializes a new instance of <see cref="DistributedTokenCacheService"/>
        /// </summary>
        /// <param name="loggerFactory"><see cref="ILoggerFactory"/> used to create type-specific <see cref="ILogger"/> instances.</param>
        /// <param name="dataProtectionProvider">An <see cref="IDataProtectionProvider"/> for creating a data protector.</param>
        public DistributedTokenCacheService(
            IDistributedCache distributedCache,
            ILoggerFactory loggerFactory,
            IDataProtectionProvider dataProtectionProvider)
            : base(loggerFactory)
        {
            Guard.ArgumentNotNull(distributedCache, nameof(distributedCache));
            Guard.ArgumentNotNull(dataProtectionProvider, nameof(dataProtectionProvider));
            _distributedCache = distributedCache;
            _dataProtectionProvider = dataProtectionProvider;
        }

        /// <summary>
        /// Returns an instance of <see cref="TokenCache"/>.
        /// </summary>
        /// <param name="claimsPrincipal">Current user's <see cref="ClaimsPrincipal"/>.</param>
        /// <returns>An instance of <see cref="TokenCache"/>.</returns>
        public override Task<TokenCache> GetCacheAsync(ClaimsPrincipal claimsPrincipal)
        {
            if (_cache == null)
            {
                _cache = new DistributedTokenCache(claimsPrincipal, _distributedCache, _loggerFactory, _dataProtectionProvider);
            }

            return Task.FromResult(_cache);
        }
    }
}
