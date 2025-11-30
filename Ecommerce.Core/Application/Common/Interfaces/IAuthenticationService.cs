using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.Wrapper;

namespace Ecommerce.Core.Application.Common.Interfaces;

public interface IAuthenticationService
{
    Task<ResponseType<AuthenticationResponseDto>> Login(LoginRequestDto request);
    Task<ResponseType<string>> Logout(string userId);
}