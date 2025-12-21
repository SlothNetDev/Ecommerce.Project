namespace Ecommerce.Shared.Enums;

/// <summary>
/// Possible HTTP STATUS CODES That likely failed 
/// </summary>
public enum FailureType
{
    Validation,
    Authentication,
    Authorization,
    NotFound,
    Conflict,
    RateLimited,
    Expired,
    Locked,
    Internal
}