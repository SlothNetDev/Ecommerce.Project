using Ecommerce.Core.Application.Common.Interfaces.Notification;
using Ecommerce.Core.Application.Common.Interfaces.Register;
using Ecommerce.Infrastructure.Data.Seeders;
using Ecommerce.Infrastructure.Identity.Entities;
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
                if (!existingUser.EmailConfirmed)
                {
                    logger.LogInformation("REG_002: Re-registering unconfirmed user: {Email}", request.Email);
                    await otpService.RemoveOtpAsync(request.Email);
                }
                else
                {
                    return ResponseType<RegisterResponseDto>.Fail(
                        "Email Already Exists",
                        FailureType.Conflict,
                        "An account with this email already exists.");
                }
            }

            // 3. Create user
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                EmailConfirmed = false,
                AccountCreatedAt = DateTime.UtcNow
            };

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

            // 4. Assign default role
            var roleResult = await userManager.AddToRoleAsync(user, RoleSeeder.Customer);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                logger.LogError("REG_004: Role assignment failed for {UserId}: {Errors}", user.Id, errors);
                await userManager.DeleteAsync(user);
                return ResponseType<RegisterResponseDto>.Fail("RoleAssignmentFailed",
                    FailureType.Authorization,
                     errors);
            }

            // 5. Generate OTP internally
            var otpCode = await otpService.GenerateOtpAsync(user.Email);
            if (string.IsNullOrWhiteSpace(otpCode) || otpCode.Length != 6)
            {
                logger.LogError("REG_005: OTP generation failed for {Email}", user.Email);
                await userManager.DeleteAsync(user);
                return ResponseType<RegisterResponseDto>.Fail(
                    "OtpGenerationFailed",
                    FailureType.Internal,
                    "Failed to generate verification code. Please try again.");
            }

            // 6. Enqueue OTP email internally
            var userName = $"{user.FirstName} {user.LastName}".Trim();
            BackgroundJob.Enqueue<IEmailService>(email =>
                email.SendOtpEmailAsync(user.Email, otpCode, userName));

            logger.LogInformation("REG_006: User registered successfully, OTP enqueued for {Email} (ID: {UserId})",
                user.Email, user.Id);

            // 7. Return response without exposing OTP
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
        var validationResult = await otpService.ValidateOtpAsync(request.Email, request.VerificationCode);
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
        BackgroundJob.Enqueue<IEmailService>(email =>
            email.SendWelcomeEmailAsync(user.Email, user.UserName, user.FirstName));

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