namespace Ecommerce.Core.Application.Common.Interfaces.RefreshToken;

/// <summary>
/// Provides methods for handling IP address related operations, such as retrieving the client's IP,
/// detecting suspicious addresses, and obtaining geolocation data.
/// </summary>
public interface IIpAdressService
{
    /// <summary>
    /// Retrieves the IP address of the current client making the request.
    /// </summary>
    /// <returns>The client's IP address as a string.</returns>
    string GetClientIpAddress();

    /// <summary>
    /// Checks if the provided IP address is considered suspicious or potentially malicious.
    /// </summary>
    /// <param name="ip">The IP address to evaluate.</param>
    /// <returns>True if the IP is suspicious, otherwise false.</returns>
    bool IsSuspiciousIp(string ip);

    /// <summary>
    /// Asynchronously fetches the approximate location information for the given IP address.
    /// </summary>
    /// <param name="ip">The IP address to lookup.</param>
    /// <returns>A string describing the geolocation or a related address for the IP.</returns>
    Task<string> GetLocationAsync(string ip);
}