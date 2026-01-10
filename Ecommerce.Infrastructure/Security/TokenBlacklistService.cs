using Ecommerce.Core.Application.Common.Interfaces.Security;
using Microsoft.Extensions.Caching.Memory;

namespace Ecommerce.Infrastructure.Security;

public class TokenBlacklistService(IMemoryCache cache) : ITokenBlacklistService
{
    public Task BlackListTokenAsync(string jti, TimeSpan expiresIn)
    {
        if(string.IsNullOrEmpty(jti) || expiresIn < TimeSpan.Zero)
            throw new ArgumentNullException(nameof(jti));
        
        cache.Set($"blacklist_{jti}", true, expiresIn);
        return Task.CompletedTask;
    }

    public Task<bool> IsTokenBlacklistedAsync(string jti)
    {
        if (string.IsNullOrEmpty(jti))
            throw new ArgumentNullException(nameof(jti));
        
        return Task.FromResult(cache.TryGetValue($"blacklist_{jti}", out _));
    }
}