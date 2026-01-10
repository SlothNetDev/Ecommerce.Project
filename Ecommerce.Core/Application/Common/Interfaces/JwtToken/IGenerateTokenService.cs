using System.Security.Claims;
using Ecommerce.Shared.TokenDTO;

namespace Ecommerce.Core.Application.Common.Interfaces.JwtToken;

/// <summary>
/// Defines operations for generating and managing JWT tokens in production authentication workflows.
/// This service enables the secure creation of JWT tokens and construction of user claims 
/// required for token-based authentication.
/// </summary>
public interface IGenerateTokenService
{
    /// <summary>
    /// Creates a JWT token containing the specified user claims.
    /// Intended for use in production scenarios to support stateless authentication.
    /// </summary>
    /// <param name="claims">Collection of claims to embed in the JWT payload.</param>
    /// <returns>
    /// The generated JWT token as a string, ready for inclusion in authentication responses.
    /// </returns>
    string CreateJwtToken(IEnumerable<Claim> claims);

    /// <summary>
    /// Builds and returns the set of standard and custom claims for a given user.
    /// These claims are embedded in the JWT to convey user identity and authorization info.
    /// </summary>
    /// <param name="user">DTO containing user information to incorporate into claims.</param>
    /// <returns>
    /// An enumerable collection of Claim objects suitable for JWT issuance in production.
    /// </returns>
    IEnumerable<Claim> BuildClaims(TokenUserDto user);
}