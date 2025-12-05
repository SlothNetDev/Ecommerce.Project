using Ecommerce.Core.Application.Common.Interfaces.RefreshToken;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Identity.Services;

// In Infrastructure/Services
public class IpAddressService : IIpAdressService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<IpAddressService> _logger;
    private readonly IConfiguration _configuration;
    
    // CRITICAL: Define trusted proxies to prevent IP spoofing
    private readonly HashSet<string> _trustedProxies;

    public IpAddressService(
        IHttpContextAccessor httpContextAccessor,
        ILogger<IpAddressService> logger,
        IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _configuration = configuration;
        
        // 1. Load trusted proxy IPs from configuration
        // These are YOUR proxies (Azure Gateway, Nginx, Cloudflare, etc.)
        // Format in appsettings: "TrustedProxies": ["10.0.0.1", "172.16.0.1"]
        var trustedProxiesConfig = _configuration.GetSection("TrustedProxies").Get<string[]>();
        _trustedProxies = trustedProxiesConfig != null 
            ? new HashSet<string>(trustedProxiesConfig) 
            : new HashSet<string>();
    }

    // Level 2 Security: Proper IP detection with anti-spoofing
    public string GetClientIpAddress()
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
            {
                _logger.LogWarning("HttpContext is null when getting IP");
                return "Unknown";
            }

            var headers = context.Request.Headers;
            var connectionIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            // 2. Verify the immediate connection is from a trusted proxy
            // If not trusted, someone could be spoofing headers - use connection IP only
            if (!IsTrustedProxy(connectionIp))
            {
                _logger.LogDebug("Connection from untrusted source: {IP}, using connection IP", connectionIp);
                return connectionIp;
            }

            // 3. Connection is trusted, now check proxy headers
            // X-Forwarded-For contains chain: clientIP, proxy1, proxy2
            // We want the FIRST IP (leftmost) = real client
            if (headers.TryGetValue("X-Forwarded-For", out var xForwardedFor))
            {
                var forwardedIps = xForwardedFor.ToString()
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(ip => ip.Trim())
                    .ToArray();

                // 4. Get first IP in chain (real client)
                if (forwardedIps.Length > 0)
                {
                    var clientIp = forwardedIps[0];
                    
                    if (IsValidIp(clientIp))
                    {
                        _logger.LogDebug("Got client IP from X-Forwarded-For: {IP}", clientIp);
                        return clientIp;
                    }
                    else
                    {
                        _logger.LogWarning("Invalid IP in X-Forwarded-For: {IP}", clientIp);
                    }
                }
            }

            // 5. Fallback to X-Real-IP (simpler proxy header)
            if (headers.TryGetValue("X-Real-IP", out var xRealIp))
            {
                var realIp = xRealIp.ToString().Trim();
                if (IsValidIp(realIp))
                {
                    _logger.LogDebug("Got client IP from X-Real-IP: {IP}", realIp);
                    return realIp;
                }
            }

            // 6. No valid proxy headers, use connection IP (already trusted)
            _logger.LogDebug("No proxy headers found, using connection IP: {IP}", connectionIp);
            return connectionIp;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting client IP address");
            return "Unknown";
        }
    }

    // Level 2: Enhanced suspicious IP detection
    public bool IsSuspiciousIp(string ip)
    {
        if (string.IsNullOrEmpty(ip) || ip == "Unknown")
        {
            // 1. Unknown IPs are suspicious by default - block them
            return true;
        }

        // 2. Private IPs are not suspicious (internal traffic)
        if (IsPrivateIp(ip))
        {
            return false;
        }

        // 3. Check if IP is actually valid format
        if (!IsValidIp(ip))
        {
            _logger.LogWarning("Invalid IP format detected: {IP}", ip);
            return true;
        }

        // 4. Check for known malicious patterns
        // These are EXAMPLES - customize based on your threat intelligence
        var suspiciousPatterns = new[]
        {
            // Tor exit nodes often start with specific ranges
            @"^185\.220\.", 
            // Add patterns from security feeds you trust
        };

        foreach (var pattern in suspiciousPatterns)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(ip, pattern))
            {
                _logger.LogWarning("Suspicious IP pattern matched: {IP} (Pattern: {Pattern})", ip, pattern);
                return true;
            }
        }

        // 5. Optional: Check against your own blocklist in database
        // var isBlocked = await _dbContext.BlockedIps.AnyAsync(b => b.IpAddress == ip);
        // if (isBlocked) return true;

        return false;
    }

    // Level 2: Basic geolocation (placeholder for future enhancement)
    public async Task<string> GetLocationAsync(string ip)
    {
        // 1. Skip lookup for invalid or private IPs
        if (string.IsNullOrEmpty(ip) || ip == "Unknown" || IsPrivateIp(ip))
        {
            return "Local";
        }

        try
        {
            // 2. For Level 2, we keep this simple
            // Options for production:
            // - MaxMind GeoLite2 (free, local database - RECOMMENDED for Level 2)
            // - ip-api.com (free tier: 45 req/min)
            // - ipinfo.io (free tier: 50k req/month)
            
            // 3. Placeholder implementation
            // TODO: Integrate actual geolocation service when needed
            // For e-commerce: country code is enough for tax/currency
            // For fraud detection: country + city increases accuracy
            
            return await Task.FromResult("Unknown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting location for IP: {IP}", ip);
            return "Unknown";
        }
    }

    // === PRIVATE HELPER METHODS ===

    // Check if IP is from your trusted infrastructure
    private bool IsTrustedProxy(string ip)
    {
        // 1. If no trusted proxies configured, trust nothing (secure default)
        if (_trustedProxies.Count == 0)
        {
            _logger.LogWarning("No trusted proxies configured - treating all as untrusted");
            return false;
        }

        // 2. Check exact match first
        if (_trustedProxies.Contains(ip))
        {
            return true;
        }

        // 3. Check for CIDR ranges if configured (e.g., "10.0.0.0/8")
        // This is advanced - for Level 2, exact IPs are usually enough
        // TODO: Implement CIDR matching if you have IP ranges

        return false;
    }

    // Validate IP address format (both IPv4 and IPv6)
    private bool IsValidIp(string ip)
    {
        // 1. Basic null/whitespace check
        if (string.IsNullOrWhiteSpace(ip))
        {
            return false;
        }

        // 2. Use .NET parser to validate format
        // This handles both IPv4 (192.168.1.1) and IPv6 (2001:0db8::1)
        return System.Net.IPAddress.TryParse(ip, out _);
    }

    // Check if IP is private/internal (RFC 1918)
    private bool IsPrivateIp(string ip)
    {
        if (string.IsNullOrEmpty(ip))
        {
            return false;
        }

        // 1. Parse IP to check ranges properly
        if (!System.Net.IPAddress.TryParse(ip, out var ipAddress))
        {
            return false;
        }

        var bytes = ipAddress.GetAddressBytes();

        // 2. IPv4 private ranges
        if (bytes.Length == 4)
        {
            // 10.0.0.0/8
            if (bytes[0] == 10)
            {
                return true;
            }

            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            {
                return true;
            }

            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168)
            {
                return true;
            }

            // 127.0.0.0/8 (localhost)
            if (bytes[0] == 127)
            {
                return true;
            }
        }

        // 3. IPv6 localhost and private
        if (ipAddress.IsIPv6LinkLocal || 
            ipAddress.IsIPv6SiteLocal || 
            System.Net.IPAddress.IsLoopback(ipAddress))
        {
            return true;
        }

        return false;
    }
}