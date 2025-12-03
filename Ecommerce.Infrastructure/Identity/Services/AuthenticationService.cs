using Ecommerce.Core.Application.Common.Interfaces;
using Ecommerce.Core.Application.Settings;
using Ecommerce.Core.Domain.Entities.UserManagement;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.Wrapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecommerce.Infrastructure.Identity.Services;

public class AuthenticationService(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    IOptions<IdentitySettings> identity,
    ILogger<AuthenticationService> logger) : IAuthenticationService
{
    public async Task<ResponseType<AuthenticationResponseDto>> LoginAsync(LoginRequestDto request, string ipAddress)
    {
        //1. find if emails exist
        var email = userManager.FindByEmailAsync(request.Email).Result;
        if (email == null)
        {
            logger.LogError($"User with email {request.Email} not found");
            return ResponseType<AuthenticationResponseDto>.Fail("Invalid name of password");
        }
            
        //2. check if password was correct and built in method
        var passwordValid = await userManager.CheckPasswordAsync(email, request.Password);

        if (!passwordValid)
        {
            logger.LogError($"User with email {request.Email} not found");
            return ResponseType<AuthenticationResponseDto>.Fail("Invalid name of password");
        }
        
        //3. Get the roles assigned based on their role
        var roles 
        
    }

    public async Task<ResponseType<string>> LogoutAsync(string userId, string ipAddress)
    {
        throw new NotImplementedException();
    }
    
}