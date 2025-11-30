using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.Wrapper;

namespace Ecommerce.Core.Application.Common.Interfaces;

public interface IUserRegistrationService
{
    Task<ResponseType<string>> Register(RegisterRequestDto request);
    Task<ResponseType<string>> ConfirmEmail(string token, string email);
    Task<ResponseType<string>> ForgotPassword(string email);
}