using Ecommerce.Core.Application.Common.Interfaces.BackGroundJob;
using Ecommerce.Core.Application.Common.Interfaces.Notification;
using Ecommerce.Core.Application.Common.Interfaces.Register;
using Ecommerce.Core.Domain.Entities.UserManagement;
using Ecommerce.Infrastructure.Data.Seeders;
using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Infrastructure.Services.BackgroundJobs;
using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.Enums;
using Ecommerce.Shared.RegisterDto;
using Ecommerce.Shared.Wrapper;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Identity.Services.Register;

/// <summary>
/// Service handling user registration with modern OTP email verification.
/// Implements the complete registration workflow including account creation,
/// OTP generation, email verification, and account activation.
/// </summary>
public class UserRegistrationService(
    UserManager<ApplicationUser> userManager,
    IOtpService otpService,
    IEmailService emailService,
    ILogger<UserRegistrationService> logger)
    : IUserRegistrationService
{
    /// <summary>
    /// Registers a new user with OTP email verification.
    /// Creates account with EmailConfirmed = false, generates OTP, and sends verification email.
    /// </summary>
    public async Task<ResponseType<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request)
    {
        // 1. Validate password
            if (request.Password != request.ConfirmPassword)
            {
                logger.LogWarning("REG_001: Password mismatch for {Email}", request.Email);
                return ResponseType<RegisterResponseDto>.Fail(
                    "PasswordMismatch",
                    FailureType.Authentication,
                    "Password and confirmation password do not match.");
            }

            // 2. Check if email exists
            var existingUser = await userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                logger.LogWarning("REG_002: Email already exists for {Email}", request.Email);
                return ResponseType<RegisterResponseDto>
                    .Fail( "Email Already Exists", FailureType.Conflict, 
                        "An account with this email already exists.");
            }
            
            // 3. Create domain user
            var domainUser = new User();

            // 4. Create user
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                EmailConfirmed = false,
                AccountCreatedAt = DateTime.UtcNow,
                DomainUser = domainUser,
                SecurityStamp = Guid.NewGuid().ToString(),
                LockoutEnabled = true,
                AccessFailedCount = 0
            };
            
            
            //5. create the user in the database 
            var createResult = await userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                logger.LogError("REG_003: User creation failed for {Email}: {Errors}", request.Email, errors);
                return ResponseType<RegisterResponseDto>.Fail("Registration Failed",
                    FailureType.Authentication,
                    errors
                );
            }
            // 6. Assign default role and in database
            var roleResult = await userManager.AddToRoleAsync(user, RoleSeeder.Customer);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                logger.LogError("REG_004: Role assignment failed for {UserId}: {Errors}", user.Id, errors);
                await userManager.DeleteAsync(user);
                return ResponseType<RegisterResponseDto>.Fail("Role Assignment Failed",
                    FailureType.Authorization,
                    errors);
            }
            
            
            // 7. Generate OTP internally
            var otpCode = await otpService.CreateEmailOtpAsync(user.Id);
            if (string.IsNullOrWhiteSpace(otpCode.Data))
            {
                logger.LogError("REG_005: OTP generation failed for {Email}", user.Email);
                await userManager.DeleteAsync(user);
                return ResponseType<RegisterResponseDto>.Fail(
                    "OtpGenerationFailed",
                    FailureType.Internal,
                    "Failed to generate verification code. Please try again.");
            }

            // 8. Enqueue OTP email internally
            var userName = $"{user.FirstName} {user.LastName}".Trim();
            BackgroundJob.Enqueue<EmailJobService>(email =>
                email.SendOtpAsync(user.Email, otpCode.Data, userName));
            
            // 9. Return response without exposing OTP
            var response = new RegisterResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                EmailVerified = false,
                Message = "Account created successfully. Please check your email for the verification code.",
                ExpiresAt = DateTime.UtcNow.AddMinutes(10) // OTP expiration
            };

            return ResponseType<RegisterResponseDto>.SuccessResult(response, "Successfully registered your account");
    }

    public async Task<ResponseType<string>> VerifyEmailAsync(EmailVerificationRequestDto request)
    {
        var validationResult = await otpService.VerifyEmailOtpAsync(request.UserId,request.VerificationCode);
        switch (validationResult)
        {
            case OtpValidationResult.Valid:
                break;
            case OtpValidationResult.Invalid:
                return ResponseType<string>.Fail("InvalidCode", FailureType.Validation, "Verification code is incorrect.");
            case OtpValidationResult.Expired:
                return ResponseType<string>.Fail("CodeExpired", FailureType.Validation, "Verification code expired.");
            case OtpValidationResult.TooManyAttempts:
                return ResponseType<string>.Fail("TooManyAttempts", FailureType.Validation, "Too many attempts. Please wait before retrying.");
            case OtpValidationResult.NotFound:
            default:
                return ResponseType<string>.Fail("CodeNotFound", FailureType.NotFound, "No OTP found. Please register first.");
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return ResponseType<string>.Fail("UserNotFound", FailureType.NotFound, "User account not found.");

        // Confirm email internally
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmResult = await userManager.ConfirmEmailAsync(user, token);
        if (!confirmResult.Succeeded)
        {
            var errors = string.Join(", ", confirmResult.Errors.Select(e => e.Description));
            logger.LogError("VER_001: Email confirmation failed for {UserId}: {Errors}", user.Id, errors);
            return ResponseType<string>.Fail("ConfirmationFailed", FailureType.Internal, "Email confirmation failed.");
        }

        // Send welcome email asynchronously
        var userName = $"{user.FirstName} {user.LastName}".Trim();
        BackgroundJob.Enqueue<EmailJobService>(email =>
            email.SendWelcomeAsync(user.Email!, user.UserName!, user.FirstName));

        return ResponseType<string>.SuccessResult("Email verified successfully! Your account is now active.");
    }


    public Task<ResponseType<PasswordResetRequestDto>> ForgotPasswordAsync(string email)
    {
        // TODO: Implement password reset workflow
        logger.LogInformation("FORGOT_001: Password reset requested for {Email}", email);
        var response = new PasswordResetRequestDto
        {
            Email = email,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            BackgroundJobId = null
        };
        return Task.FromResult(ResponseType<PasswordResetRequestDto>.SuccessResult(response, "Password reset will be implemented soon."));
    }

    [Obsolete("Please use VerifyEmailAsync for OTP-based email verification.")]
    public Task<ResponseType<string>> ConfirmEmailAsync(string token, string email)
    {
        logger.LogWarning("LEGACY_001: Legacy ConfirmEmail called for {Email}", email);
        return Task.FromResult(ResponseType<string>.Fail("MethodDeprecated", 
            FailureType.Authentication,
            "Use VerifyEmailAsync with OTP codes."));
    }

   
}