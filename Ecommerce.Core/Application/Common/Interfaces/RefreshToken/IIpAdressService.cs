namespace Ecommerce.Core.Application.Common.Interfaces.RefreshToken;

public interface IIpAdressService
{
    string GetClientIpAddress();
    bool IsSuspiciousIp(string ip);
    Task<string> GetLocationAsync(string ip);
}