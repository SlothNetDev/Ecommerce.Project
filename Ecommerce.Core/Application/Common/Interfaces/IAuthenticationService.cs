using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.Wrapper;

namespace Ecommerce.Core.Application.Common.Interfaces;

public interface IAuthenticationService
{
   
    Task<ResponseType<AuthenticationResponseDto>> LoginAsync(LoginRequestDto request, string ipAddress);
    Task<ResponseType<string>> LogoutAsync(string userId, string ipAddress);
}
