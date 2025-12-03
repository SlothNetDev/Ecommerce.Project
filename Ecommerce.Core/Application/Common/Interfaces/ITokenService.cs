using System.Security.Claims;
using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.TokenDTO;
using Ecommerce.Shared.Wrapper;

namespace Ecommerce.Core.Application.Common.Interfaces;

public interface ITokenService
{
    string CreateJwtToken(IEnumerable<Claim> claims);
    IEnumerable<Claim> BuildClaims(TokenUserDto user);
}