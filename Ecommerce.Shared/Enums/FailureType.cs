namespace Ecommerce.Shared.Enums;

/// <summary>
/// High-level failure categories used to map domain errors
/// to transport-level responses (HTTP, gRPC, messaging).
///
/// IMPORTANT:
/// - This enum is NOT a mirror of HTTP status codes.
/// - Choose the type based on what the CLIENT is allowed to know,
///   not what the system internally discovered.
/// </summary>
public enum FailureType
{
    /// <summary>
    /// The request data is syntactically or semantically invalid.
    /// Examples:
    /// - Missing required fields
    /// - Invalid format (email, phone)
    /// - Business validation rules
    ///
    /// Maps to: HTTP 400
    /// </summary>
    Validation,

    /// <summary>
    /// Authentication failed.
    /// Use when:
    /// - Invalid credentials (wrong password OR user not found)
    /// - Invalid, expired, or malformed tokens
    ///
    /// IMPORTANT:
    /// - Do NOT leak whether a user exists.
    ///
    /// Maps to: HTTP 401
    /// </summary>
    Authentication,

    /// <summary>
    /// The authenticated user does not have permission
    /// to perform this action.
    ///
    /// Examples:
    /// - Insufficient role/claims
    /// - Account disabled
    ///
    /// Maps to: HTTP 403
    /// </summary>
    Authorization,

    /// <summary>
    /// The requested resource does not exist OR is not visible
    /// to the current user.
    ///
    /// Examples:
    /// - Product ID does not exist
    /// - Order not owned by the user
    ///
    /// NOTE:
    /// - Should NOT be used for authentication flows.
    ///
    /// Maps to: HTTP 404
    /// </summary>
    NotFound,

    /// <summary>
    /// The request conflicts with the current state of the system.
    ///
    /// Examples:
    /// - Email already registered
    /// - Unique constraint violation
    /// - Optimistic concurrency failure
    ///
    /// Maps to: HTTP 409
    /// </summary>
    Conflict,

    /// <summary>
    /// The client has sent too many requests in a given time window.
    ///
    /// Examples:
    /// - Login attempts exceeded
    /// - API rate limit hit
    ///
    /// Maps to: HTTP 429
    /// </summary>
    RateLimited,

    /// <summary>
    /// The operation failed because a time-bound constraint expired.
    ///
    /// Examples:
    /// - Refresh token expired
    /// - Password reset token expired
    ///
    /// Maps to: HTTP 401 (or 410 in rare cases)
    /// </summary>
    Expired,

    /// <summary>
    /// The account or resource is temporarily locked.
    ///
    /// Examples:
    /// - Too many failed login attempts
    /// - Administrative lock
    ///
    /// Maps to: HTTP 423 (or 403 if 423 is not supported)
    /// </summary>
    Locked,

    /// <summary>
    /// An unexpected error occurred.
    ///
    /// Examples:
    /// - Unhandled exception
    /// - Database outage
    ///
    /// Maps to: HTTP 500
    /// </summary>
    Internal
}
