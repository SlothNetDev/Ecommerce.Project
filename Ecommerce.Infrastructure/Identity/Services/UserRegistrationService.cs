using Ecommerce.Core.Application.Common.Interfaces.Notification;
using Ecommerce.Core.Application.Common.Interfaces.Register;
using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Shared.Enums;
using Ecommerce.Shared.RegisterDto;
using Ecommerce.Shared.Wrapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Identity.Services;

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
        try
        {
            // 1. Validate passwords match
            if (request.Password != request.ConfirmPassword)
            {
                logger.LogWarning("REG_001: Password confirmation failed for email: {Email}", request.Email);
                return ResponseType<RegisterResponseDto>.Fail(
                    "PasswordMismatch",
                    "Password and confirmation password do not match.");
            }

            // 2. Check if email already exists
            var existingUser = await userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                // If user exists but email is not confirmed, allow re-registration
                if (!existingUser.EmailConfirmed)
                {
                    logger.LogInformation("REG_002: Re-registering unconfirmed user: {Email}", request.Email);
                    // Clean up any existing OTP for this email
                    await otpService.RemoveOtpAsync(request.Email);
                }
                else
                {
                    logger.LogWarning("REG_003: Attempted registration with existing confirmed email: {Email}", request.Email);
                    return ResponseType<RegisterResponseDto>.Fail(
                        "EmailAlreadyExists",
                        "An account with this email already exists.");
                }
            }

            // 3. Create new user account with EmailConfirmed = false
            var user = new ApplicationUser
            {
                UserName = request.Email, // Use email as username for simplicity
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                EmailConfirmed = false, // Will be confirmed via OTP
                AccountCreatedAt = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(user, request.Password);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                logger.LogError("REG_004: User creation failed for {Email}: {Errors}", request.Email, errors);
                return ResponseType<RegisterResponseDto>.Fail(
                    "RegistrationFailed",
                    $"Account creation failed: {errors}");
            }

            // 4. Assign default "Customer" role
            var roleResult = await userManager.AddToRoleAsync(user, "Costumer");
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                logger.LogError("REG_005: Role assignment failed for user {UserId}: {Errors}", user.Id, errors);

                // Clean up: delete the user if role assignment fails
                await userManager.DeleteAsync(user);
                return ResponseType<RegisterResponseDto>.Fail(
                    "RegistrationFailed",
                    "Failed to assign user role. Please try again.");
            }

            // 5. Generate OTP
            var otpGenerated = await otpService.GenerateOtpAsync(request.Email);
            if (!otpGenerated)
            {
                logger.LogError("REG_006: OTP generation failed for {Email}", request.Email);
                // Clean up: delete the user if OTP generation fails
                await userManager.DeleteAsync(user);
                return ResponseType<RegisterResponseDto>.Fail(
                    "OtpGenerationFailed",
                    "Failed to generate verification code. Please try again.");
            }

            // 6. Send OTP email (Note: In production, we'd modify email service to handle OTP internally)
            var userName = $"{request.FirstName} {request.LastName}".Trim();
            var emailSent = await SendOtpEmailWithCodeAsync(request.Email, userName);

            if (!emailSent)
            {
                logger.LogWarning("REG_007: OTP email sending failed for {Email}", request.Email);
                // Note: We don't delete the user here as they might try verification manually
                // In production, you might want to implement a retry mechanism or queue system
            }

            logger.LogInformation("REG_008: User registered successfully, OTP sent: {Email} (ID: {UserId})",
                request.Email, user.Id);

            // 7. Return success response with verification required status
            var response = new RegisterResponseDto
            {
                UserId = user.Id,
                Email = user.Email!,
                EmailVerified = false,
                Message = "Account created successfully. Please check your email for the verification code.",
                ExpiresAt = DateTime.UtcNow.AddMinutes(10) // OTP expiration time
            };

            return ResponseType<RegisterResponseDto>.SuccessResult(response,"Successfully registered your Account");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "REG_009: Unexpected error during registration for {Email}", request.Email);
            return ResponseType<RegisterResponseDto>.Fail(
                "RegistrationError",
                "An unexpected error occurred during registration. Please try again.");
        }
    }

    /// <summary>
    /// Verifies the user's email using the provided OTP code.
    /// Activates the account upon successful verification.
    /// </summary>
    public async Task<ResponseType<string>> VerifyEmailAsync(EmailVerificationRequestDto request)
    {
        try
        {
            // 1. Validate OTP
            var validationResult = await otpService.ValidateOtpAsync(request.Email, request.VerificationCode);

            switch (validationResult)
            {
                case OtpValidationResult.Valid:
                    // OTP is valid, proceed with email confirmation
                    break;

                case OtpValidationResult.Invalid:
                    logger.LogWarning("VER_001: Invalid OTP code for email: {Email}", request.Email);
                    return ResponseType<string>.Fail(
                        "InvalidCode",
                        "The verification code is incorrect. Please check and try again.");

                case OtpValidationResult.Expired:
                    logger.LogWarning("VER_002: Expired OTP for email: {Email}", request.Email);
                    return ResponseType<string>.Fail(
                        "CodeExpired",
                        "The verification code has expired. Please request a new one.");

                case OtpValidationResult.TooManyAttempts:
                    logger.LogWarning("VER_003: Too many OTP attempts for email: {Email}", request.Email);
                    return ResponseType<string>.Fail(
                        "TooManyAttempts",
                        "Too many failed attempts. Please wait before trying again.");

                case OtpValidationResult.NotFound:
                default:
                    logger.LogWarning("VER_004: No OTP found for email: {Email}", request.Email);
                    return ResponseType<string>.Fail(
                        "CodeNotFound",
                        "No verification code found. Please register first or request a new code.");
            }

            // 2. Find and confirm the user
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                logger.LogError("VER_005: User not found for verified email: {Email}", request.Email);
                return ResponseType<string>.Fail(
                    "UserNotFound",
                    "Account not found. Please register first.");
            }

            // 3. Confirm email
            var confirmResult = await userManager.ConfirmEmailAsync(user, await GenerateEmailConfirmationTokenAsync(user));
            if (!confirmResult.Succeeded)
            {
                var errors = string.Join(", ", confirmResult.Errors.Select(e => e.Description));
                logger.LogError("VER_006: Email confirmation failed for user {UserId}: {Errors}", user.Id, errors);
                return ResponseType<string>.Fail(
                    "ConfirmationFailed",
                    "Email confirmation failed. Please contact support.");
            }

            // 4. Send welcome email
            var userName = $"{user.FirstName} {user.LastName}".Trim();
            await emailService.SendWelcomeEmailAsync(user.Email!, user.UserName!, user.FirstName);

            logger.LogInformation("VER_007: Email verified successfully for user: {Email} (ID: {UserId})",
                request.Email, user.Id);

            return ResponseType<string>.SuccessResult(
                "Email verified successfully! Your account is now active and you can log in.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "VER_008: Unexpected error during email verification for {Email}", request.Email);
            return ResponseType<string>.Fail(
                "VerificationError",
                "An unexpected error occurred during verification. Please try again.");
        }
    }

    /// <summary>
    /// Legacy method for backward compatibility.
    /// Now delegates to the new OTP-based verification.
    /// </summary>
    public async Task<ResponseType<string>> ConfirmEmail(string token, string email)
    {
        // This method is kept for backward compatibility
        // In modern implementation, email confirmation happens via OTP
        logger.LogWarning("LEGACY_001: Legacy ConfirmEmail called for {Email}", email);
        return ResponseType<string>.Fail(
            "MethodDeprecated",
            "Please use the new email verification process with OTP codes.");
    }

    /// <summary>
    /// Initiates password reset with OTP (placeholder implementation).
    /// </summary>
    public async Task<ResponseType<string>> ForgotPassword(string email)
    {
        // TODO: Implement password reset with OTP
        logger.LogInformation("FORGOT_001: Password reset requested for {Email}", email);
        return ResponseType<string>.SuccessResult(
            "Password reset functionality will be implemented soon.");
    }

    /// <summary>
    /// Sends OTP email with the verification code.
    /// DEVELOPMENT/TESTING ONLY: This method demonstrates the concept but is not production-ready.
    /// In production, implement proper email templating where OTP is embedded server-side.
    /// </summary>
    private async Task<bool> SendOtpEmailWithCodeAsync(string email, string userName)
    {
        try
        {
            // WARNING: This is a DEVELOPMENT/TESTING workaround
            // In production, you should:
            // 1. Store OTP in a secure way that can be retrieved by the email service
            // 2. Use email templates with server-side OTP embedding
            // 3. Never expose OTP codes in logs or responses

            // For now, we'll generate a demo OTP and send it
            // This simulates what would happen in a real email service
            var demoOtpCode = "123456"; // In production, this would come from secure storage

            logger.LogWarning("DEV_MODE: Sending demo OTP {Code} to {Email}. In production, use secure OTP retrieval.",
                demoOtpCode, email);

            return await emailService.SendOtpEmailAsync(email, demoOtpCode, userName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send OTP email to {Email}", email);
            return false;
        }
    }

    /// <summary>
    /// Generates an email confirmation token for Identity.
    /// </summary>
    private async Task<string> GenerateEmailConfirmationTokenAsync(ApplicationUser user)
    {
        return await userManager.GenerateEmailConfirmationTokenAsync(user);
    }
}