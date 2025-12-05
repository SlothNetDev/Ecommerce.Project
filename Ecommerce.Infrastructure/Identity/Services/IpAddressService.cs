using Ecommerce.Core.Application.Common.Interfaces.RefreshToken;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Identity.Services;

public class IpAdressService : IIpAdressService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<IpAddressService> _logger;
    private readonly IConfiguration _configuration;
    
    // CRITICAL: Define trusted proxies to prevent IP spoofing
    private readonly HashSet<string> _trustedProxies;

    public string GetClientIpAddress()
    {
        try
        {
            var context = httpContextAccessor.HttpContext;
            if(context == null) return "unknown";
            
            //check headers for proxies like Azure,Nginx and many more
            var headers = context.Request.Headers;
            
            // Try common proxy headers in order of reliability
            var proxyHeaders = new[] { "X-Forwarded-For", "X-Real-IP" };

            foreach (var header in proxyHeaders)
            {
                if(headers.TryGetValue(header, out var value))
                {
                    var ip = value.ToString();
                    if (!string.IsNullOrWhiteSpace(ip))
                    {
                        // Handle comma-separated list (client, proxy1, proxy2)
                        if (ip.Contains(','))
                        {
                            ip = ip.Split(',')[0].Trim();
                        }
                    }
                    
                    if(isvalid)
                }
            }
        }
    }

    public bool IsSuspiciousIp(string ip)
    {
        throw new NotImplementedException();
    }

    public async Task<string> GetLocationAsync(string ip)
    {
        throw new NotImplementedException();
    }
}