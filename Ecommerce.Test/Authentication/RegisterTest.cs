/*using System.Net.Http.Json;
using Ecommerce.Core.Application.Common.Interfaces.Notification;
using Ecommerce.Core.Application.Common.Interfaces.Register;
using Ecommerce.Infrastructure.Data.Seeders;
using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Infrastructure.Identity.Services.Register;
using Ecommerce.Shared.Enums;
using Ecommerce.Shared.RegisterDto;
using Ecommerce.Test.Authentication.Helpers;
using Ecommerce.Test.TestUtilities;
using Ecommerce.Api;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace Ecommerce.Test.Authentication;

/// <summary>
/// TDD Test Suite for User Registration Service
/// Tests cover: complete registration workflow, OTP generation, email sending,
/// role assignment, security validations, and error handling
/// </summary>
public class UserRegistrationServiceTest : TestBase
{
    private readonly ITestOutputHelper _output;
    private readonly AssertApiHelper _assert;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly Mock<IOtpService> _mockOtpService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ILogger<UserRegistrationService>> _mockLogger;
    private readonly Mock<IBackgroundJobClient> _mockBackgroundJobClient;
    private readonly UserRegistrationService _registrationService;
    private const string LoginEndpoint = "api/auth/register";
    public UserRegistrationServiceTest(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output) 
        : base(factory)
    {
        _output = output;
        _assert = new AssertApiHelper(output);
        
        // Get real UserManager from the factory
        _userManager = _factory.Services.GetRequiredService<UserManager<ApplicationUser>>();
        
        // Setup mocks
        _mockOtpService = new Mock<IOtpService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<UserRegistrationService>>();
        _mockBackgroundJobClient = new Mock<IBackgroundJobClient>();
        
        // Create service instance with mocks
        _registrationService = new UserRegistrationService(
            _userManager,
            _mockOtpService.Object,
            _mockEmailService.Object,
            _mockLogger.Object
        );
    }

    #region Happy Path Tests

    /// <summary>
    /// SCENARIO: New user registers with valid credentials
    /// EXPECTED: Account created, OTP generated, email queued, user unconfirmed
    /// </summary>
    [Fact(DisplayName = "Register - creates new account with OTP workflow")]
    public async Task Register_Creates_New_Account_With_OTP_Workflow()
    {
        // STEP 1: Setup test data
        var request = new RegisterRequestDto
        {
            Email = $"newuser_{Guid.NewGuid()}@test.com",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe"
        };

        _output.WriteLine($"✓ Test registration request:");
        _output.WriteLine($"  - Email: {request.Email}");
        _output.WriteLine($"  - Name: {request.FirstName} {request.LastName}");
        
        // ============================================================
        // STEP 2: Attempt login via API endpoint
        // ============================================================
        _output.WriteLine("\n--- ATTEMPTING LOGIN WITH NON-EXISTENT USER ---");
        var response = await _client.PostAsJsonAsync(LoginEndpoint, request);
        _output.WriteLine($"✓ API Response Status: {response.StatusCode}");
        
        // ============================================================
        // STEP 2: Setup mocks for successful flow
        // ============================================================
        var generatedOtp = "123456";
        _mockOtpService
            .Setup(x => x.CreateEmailOtpAsync(Guid.NewGuid()))
            .ReturnsAsync(generatedOtp);

        _mockBackgroundJobClient
            .Setup(x => x.Create(
                It.IsAny<Job>(),
                It.IsAny<EnqueuedState>()))
            .Returns("job_123");

        _output.WriteLine($"✓ Mock OTP service configured to return: {generatedOtp}");

        // ============================================================
        // STEP 3: Execute registration
        // ============================================================
        _output.WriteLine("\n--- EXECUTING REGISTRATION ---");
        var result = await _registrationService.RegisterAsync(request);

        // ============================================================
        // STEP 4: Verify successful registration
        // ============================================================
        _assert.ShouldSucceed(result);
        _output.WriteLine("✓ Registration succeeded");

        var data = result.Data!;
        Assert.NotEqual(Guid.Empty, data.UserId);
        Assert.Equal(request.Email, data.Email);
        Assert.False(data.EmailVerified);
        Assert.Contains("verification code", data.Message, StringComparison.OrdinalIgnoreCase);
        
        _output.WriteLine($"✓ Response data:");
        _output.WriteLine($"  - UserId: {data.UserId}");
        _output.WriteLine($"  - EmailVerified: {data.EmailVerified}");
        _output.WriteLine($"  - Message: {data.Message}");

        // ============================================================
        // STEP 5: Verify user exists in database
        // ============================================================
        var user = await _userManager.FindByEmailAsync(request.Email);
        Assert.NotNull(user);
        Assert.Equal(request.Email, user!.Email);
        Assert.Equal(request.FirstName, user.FirstName);
        Assert.Equal(request.LastName, user.LastName);
        Assert.False(user.EmailConfirmed);
        
        _output.WriteLine("✓ User created in database with EmailConfirmed = false");

        // ============================================================
        // STEP 6: Verify default role assigned
        // ============================================================
        
        var roles = await _userManager.GetRolesAsync(user);
        
        Assert.Contains(RoleSeeder.Customer, roles);
        _output.WriteLine("✓ Default 'Customer' role assigned");

        // ============================================================
        // STEP 7: Verify OTP was generated
        // ============================================================
        _mockOtpService.Verify(
            x => x.GenerateOtpAsync(request.Email),
            Times.Once,
            "OTP should be generated once"
        );
        _output.WriteLine("✓ OTP generation service called");

        // ============================================================
        // STEP 8: Verify email job was enqueued
        // ============================================================
        _mockBackgroundJobClient.Verify(
            x => x.Create(
                It.Is<Job>(job => 
                    job.Type == typeof(IEmailService) &&
                    job.Method.Name == nameof(IEmailService.SendOtpEmailAsync)),
                It.IsAny<EnqueuedState>()),
            Times.Once,
            "OTP email should be enqueued to Hangfire"
        );
        _output.WriteLine("✓ OTP email enqueued to Hangfire background job");

        // Cleanup
        await _userManager.DeleteAsync(user);
    }

    /// <summary>
    /// SCENARIO: User successfully verifies email with correct OTP
    /// EXPECTED: Email confirmed, welcome email sent, account activated
    /// </summary>
    [Fact(DisplayName = "VerifyEmail - activates account with correct OTP")]
    public async Task VerifyEmail_Activates_Account_With_Correct_OTP()
    {
        // ============================================================
        // STEP 1: Setup - Create unconfirmed user
        // ============================================================
        var testEmail = $"verify_{Guid.NewGuid()}@test.com";
        var testUser = new ApplicationUser
        {
            UserName = testEmail,
            Email = testEmail,
            FirstName = "Test",
            LastName = "User",
            EmailConfirmed = false,
            AccountCreatedAt = DateTime.UtcNow
        };

        await _userManager.CreateAsync(testUser, "TestPass123!");
        _output.WriteLine($"✓ Created unconfirmed test user: {testEmail}");

        // ============================================================
        // STEP 2: Setup mocks for successful verification
        // ============================================================
        var correctOtp = "123456";
        _mockOtpService
            .Setup(x => x.ValidateOtpAsync(testEmail, correctOtp))
            .ReturnsAsync(OtpValidationResult.Valid);

        _mockBackgroundJobClient
            .Setup(x => x.Create(It.IsAny<Job>(), It.IsAny<EnqueuedState>()))
            .Returns("welcome_job_123");

        // ============================================================
        // STEP 3: Execute email verification
        // ============================================================
        var verifyRequest = new EmailVerificationRequestDto
        {
            Email = testEmail,
            VerificationCode = correctOtp
        };

        _output.WriteLine("\n--- EXECUTING EMAIL VERIFICATION ---");
        var result = await _registrationService.VerifyEmailAsync(verifyRequest);

        // ============================================================
        // STEP 4: Verify successful verification
        // ============================================================
        _assert.ShouldSucceed(result);
        _output.WriteLine("✓ Email verification succeeded");
        _output.WriteLine($"✓ Success message: {result.Data}");

        // ============================================================
        // STEP 5: Verify user is now confirmed
        // ============================================================
        var updatedUser = await _userManager.FindByEmailAsync(testEmail);
        Assert.NotNull(updatedUser);
        Assert.True(updatedUser!.EmailConfirmed);
        _output.WriteLine("✓ User EmailConfirmed flag set to true");

        // ============================================================
        // STEP 6: Verify OTP validation was called
        // ============================================================
        _mockOtpService.Verify(
            x => x.ValidateOtpAsync(testEmail, correctOtp),
            Times.Once
        );
        _output.WriteLine("✓ OTP validation service called");

        // ============================================================
        // STEP 7: Verify welcome email was enqueued
        // ============================================================
        _mockBackgroundJobClient.Verify(
            x => x.Create(
                It.Is<Job>(job => 
                    job.Type == typeof(IEmailService) &&
                    job.Method.Name == nameof(IEmailService.SendWelcomeEmailAsync)),
                It.IsAny<EnqueuedState>()),
            Times.Once,
            "Welcome email should be enqueued"
        );
        _output.WriteLine("✓ Welcome email enqueued to Hangfire");

        // Cleanup
        await _userManager.DeleteAsync(updatedUser);
    }

    /// <summary>
    /// SCENARIO: Unconfirmed user re-registers with same email
    /// EXPECTED: Old OTP removed, new registration succeeds
    /// </summary>
    [Fact(DisplayName = "Register - allows re-registration for unconfirmed users")]
    public async Task Register_Allows_ReRegistration_For_Unconfirmed_Users()
    {
        // ============================================================
        // STEP 1: Create initial unconfirmed user
        // ============================================================
        var testEmail = $"retry_{Guid.NewGuid()}@test.com";
        var oldUser = new ApplicationUser
        {
            UserName = testEmail,
            Email = testEmail,
            FirstName = "Old",
            LastName = "User",
            EmailConfirmed = false
        };

        await _userManager.CreateAsync(oldUser, "OldPass123!");
        _output.WriteLine($"✓ Created initial unconfirmed user: {testEmail}");

        // ============================================================
        // STEP 2: Setup mocks
        // ============================================================
        _mockOtpService
            .Setup(x => x.RemoveOtpAsync(testEmail))
            .Returns(Task.CompletedTask);

        _mockOtpService
            .Setup(x => x.GenerateOtpAsync(testEmail))
            .ReturnsAsync("654321");

        // ============================================================
        // STEP 3: Attempt re-registration
        // ============================================================
        var request = new RegisterRequestDto
        {
            Email = testEmail,
            Password = "NewSecurePass123!",
            ConfirmPassword = "NewSecurePass123!",
            FirstName = "New",
            LastName = "User"
        };

        _output.WriteLine("\n--- ATTEMPTING RE-REGISTRATION ---");
        var result = await _registrationService.RegisterAsync(request);

        // ============================================================
        // STEP 4: Verify re-registration handled correctly
        // ============================================================
        _assert.ShouldSucceed(result);
        _output.WriteLine("✓ Re-registration succeeded");

        // ============================================================
        // STEP 5: Verify old OTP was removed
        // ============================================================
        _mockOtpService.Verify(
            x => x.RemoveOtpAsync(testEmail),
            Times.Once,
            "Old OTP should be removed for re-registration"
        );
        _output.WriteLine("✓ Old OTP removed before new registration");

        // ============================================================
        // STEP 6: Verify new OTP was generated
        // ============================================================
        _mockOtpService.Verify(
            x => x.GenerateOtpAsync(testEmail),
            Times.Once
        );
        _output.WriteLine("✓ New OTP generated for re-registration");

        // Cleanup
        var user = await _userManager.FindByEmailAsync(testEmail);
        if (user != null) await _userManager.DeleteAsync(user);
    }

    #endregion

    #region Validation & Security Tests

    /// <summary>
    /// SCENARIO: User submits mismatched passwords
    /// EXPECTED: Registration fails before database operations
    /// </summary>
    [Fact(DisplayName = "Register - rejects password mismatch (REG_001)")]
    public async Task Register_Rejects_Password_Mismatch()
    {
        // ============================================================
        // STEP 1: Setup request with mismatched passwords
        // ============================================================
        var request = new RegisterRequestDto
        {
            Email = "mismatch@test.com",
            Password = "Password123!",
            ConfirmPassword = "DifferentPassword123!",
            FirstName = "Test",
            LastName = "User"
        };

        _output.WriteLine("✓ Test request with mismatched passwords");

        // ============================================================
        // STEP 2: Execute registration
        // ============================================================
        _output.WriteLine("\n--- ATTEMPTING REGISTRATION WITH MISMATCH ---");
        var result = await _registrationService.RegisterAsync(request);

        // ============================================================
        // STEP 3: Verify rejection
        // ============================================================
        _assert.ShouldFail(result, expectedMessage: "Password and confirmation password do not match.");
        _output.WriteLine("✓ Registration correctly rejected");

        // ============================================================
        // STEP 4: Verify no database operations occurred
        // ============================================================
        var user = await _userManager.FindByEmailAsync(request.Email);
        Assert.Null(user);
        _output.WriteLine("✓ No user created in database");

        // ============================================================
        // STEP 5: Verify logging occurred
        // ============================================================
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("REG_001")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
            Times.Once
        );
        _output.WriteLine("✓ Warning logged with code REG_001");
    }

    /// <summary>
    /// SCENARIO: Attempt to register with already confirmed email
    /// EXPECTED: Registration fails to prevent account hijacking
    /// </summary>
    [Fact(DisplayName = "Register - prevents confirmed email duplication (REG_003)")]
    public async Task Register_Prevents_Confirmed_Email_Duplication()
    {
        // ============================================================
        // STEP 1: Create confirmed user
        // ============================================================
        var testEmail = $"confirmed_{Guid.NewGuid()}@test.com";
        var existingUser = new ApplicationUser
        {
            UserName = testEmail,
            Email = testEmail,
            FirstName = "Existing",
            LastName = "User",
            EmailConfirmed = true, // Already confirmed
            AccountCreatedAt = DateTime.UtcNow
        };

        await _userManager.CreateAsync(existingUser, "ExistingPass123!");
        _output.WriteLine($"✓ Created confirmed user: {testEmail}");

        // ============================================================
        // STEP 2: Attempt registration with same email
        // ============================================================
        var request = new RegisterRequestDto
        {
            Email = testEmail,
            Password = "HackPassword123!",
            ConfirmPassword = "HackPassword123!",
            FirstName = "Hacker",
            LastName = "Attempt"
        };

        _output.WriteLine("\n--- ATTEMPTING REGISTRATION WITH CONFIRMED EMAIL ---");
        var result = await _registrationService.RegisterAsync(request);

        // ============================================================
        // STEP 3: Verify rejection for security
        // ============================================================
        _assert.ShouldFail(result, expectedMessage: "An account with this email already exists.");
        _output.WriteLine("✓ Security: Registration blocked for confirmed email");

        // ============================================================
        // STEP 4: Verify original user unchanged
        // ============================================================
        var user = await _userManager.FindByEmailAsync(testEmail);
        Assert.NotNull(user);
        Assert.True(user!.EmailConfirmed);
        Assert.Equal("Existing", user.FirstName); // Original data preserved
        _output.WriteLine("✓ Original confirmed user data preserved");

        // Cleanup
        await _userManager.DeleteAsync(user);
    }

    /// <summary>
    /// SCENARIO: Register with weak password
    /// EXPECTED: Identity password policy rejects weak password
    /// </summary>
    [Fact(DisplayName = "Register - enforces password complexity (REG_004)")]
    public async Task Register_Enforces_Password_Complexity()
    {
        // ============================================================
        // STEP 1: Setup request with weak password
        // ============================================================
        var request = new RegisterRequestDto
        {
            Email = $"weak_{Guid.NewGuid()}@test.com",
            Password = "123", // Too short, no caps, no symbols
            ConfirmPassword = "123",
            FirstName = "Weak",
            LastName = "Password"
        };

        _output.WriteLine("✓ Test request with weak password: '123'");

        // ============================================================
        // STEP 2: Setup mock OTP (won't be reached)
        // ============================================================
        _mockOtpService
            .Setup(x => x.GenerateOtpAsync(It.IsAny<string>()))
            .ReturnsAsync("123456");

        // ============================================================
        // STEP 3: Execute registration
        // ============================================================
        _output.WriteLine("\n--- ATTEMPTING REGISTRATION WITH WEAK PASSWORD ---");
        var result = await _registrationService.RegisterAsync(request);

        // ============================================================
        // STEP 4: Verify rejection by Identity
        // ============================================================
        _assert.ShouldFail(result);
        _output.WriteLine("✓ Registration failed due to password policy");
        _output.WriteLine($"✓ Failure message: {result.Message}");

        // ============================================================
        // STEP 5: Verify no user created
        // ============================================================
        var user = await _userManager.FindByEmailAsync(request.Email);
        Assert.Null(user);
        _output.WriteLine("✓ No user created due to password validation");

        // ============================================================
        // STEP 6: Verify error was logged
        // ============================================================
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("REG_003")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
            Times.Once
        );
        _output.WriteLine("✓ Error logged with code REG_003");
    }

    /// <summary>
    /// SCENARIO: Verify email with incorrect OTP
    /// EXPECTED: Verification fails with InvalidCode result
    /// </summary>
    [Fact(DisplayName = "VerifyEmail - rejects incorrect OTP (VER_001)")]
    public async Task VerifyEmail_Rejects_Incorrect_OTP()
    {
        // ============================================================
        // STEP 1: Setup unconfirmed user
        // ============================================================
        var testEmail = $"wrongotp_{Guid.NewGuid()}@test.com";
        var testUser = new ApplicationUser
        {
            UserName = testEmail,
            Email = testEmail,
            EmailConfirmed = false
        };

        await _userManager.CreateAsync(testUser, "TestPass123!");
        _output.WriteLine($"✓ Created unconfirmed user: {testEmail}");

        // ============================================================
        // STEP 2: Setup mock to return Invalid
        // ============================================================
        _mockOtpService
            .Setup(x => x.ValidateOtpAsync(testEmail, "999999"))
            .ReturnsAsync(OtpValidationResult.Invalid);

        // ============================================================
        // STEP 3: Attempt verification with wrong OTP
        // ============================================================
        var request = new EmailVerificationRequestDto
        {
            Email = testEmail,
            VerificationCode = "999999"
        };

        _output.WriteLine("\n--- ATTEMPTING VERIFICATION WITH WRONG OTP ---");
        var result = await _registrationService.VerifyEmailAsync(request);

        // ============================================================
        // STEP 4: Verify rejection
        // ============================================================
        _assert.ShouldFail(result, expectedMessage: "Verification code is incorrect.");
        _output.WriteLine("✓ Verification correctly rejected");

        // ============================================================
        // STEP 5: Verify user still unconfirmed
        // ============================================================
        var user = await _userManager.FindByEmailAsync(testEmail);
        Assert.False(user!.EmailConfirmed);
        _output.WriteLine("✓ User remains unconfirmed");

        // Cleanup
        await _userManager.DeleteAsync(user);
    }

    /// <summary>
    /// SCENARIO: Verify email with expired OTP
    /// EXPECTED: Verification fails with Expired result
    /// </summary>
    [Fact(DisplayName = "VerifyEmail - rejects expired OTP (VER_002)")]
    public async Task VerifyEmail_Rejects_Expired_OTP()
    {
        // ============================================================
        // STEP 1: Setup unconfirmed user
        // ============================================================
        var testEmail = $"expired_{Guid.NewGuid()}@test.com";
        var testUser = new ApplicationUser
        {
            UserName = testEmail,
            Email = testEmail,
            EmailConfirmed = false
        };

        await _userManager.CreateAsync(testUser, "TestPass123!");
        _output.WriteLine($"✓ Created unconfirmed user: {testEmail}");

        // ============================================================
        // STEP 2: Setup mock to return Expired
        // ============================================================
        _mockOtpService
            .Setup(x => x.ValidateOtpAsync(testEmail, "123456"))
            .ReturnsAsync(OtpValidationResult.Expired);

        // ============================================================
        // STEP 3: Attempt verification
        // ============================================================
        var request = new EmailVerificationRequestDto
        {
            Email = testEmail,
            VerificationCode = "123456"
        };

        _output.WriteLine("\n--- ATTEMPTING VERIFICATION WITH EXPIRED OTP ---");
        var result = await _registrationService.VerifyEmailAsync(request);

        // ============================================================
        // STEP 4: Verify expiration handling
        // ============================================================
        _assert.ShouldFail(result, expectedMessage: "Verification code expired.");
        _output.WriteLine("✓ Expired OTP correctly rejected");

        // Cleanup
        var user = await _userManager.FindByEmailAsync(testEmail);
        await _userManager.DeleteAsync(user!);
    }

    /// <summary>
    /// SCENARIO: Verify email for non-existent user
    /// EXPECTED: Verification fails with UserNotFound
    /// </summary>
    [Fact(DisplayName = "VerifyEmail - rejects unknown user (VER_005)")]
    public async Task VerifyEmail_Rejects_Unknown_User()
    {
        // ============================================================
        // STEP 1: Setup mock to return Valid (but user doesn't exist)
        // ============================================================
        var ghostEmail = "ghost@test.com";
        _mockOtpService
            .Setup(x => x.ValidateOtpAsync(ghostEmail, "123456"))
            .ReturnsAsync(OtpValidationResult.Valid);

        // ============================================================
        // STEP 2: Attempt verification
        // ============================================================
        var request = new EmailVerificationRequestDto
        {
            Email = ghostEmail,
            VerificationCode = "123456"
        };

        _output.WriteLine("\n--- ATTEMPTING VERIFICATION FOR NON-EXISTENT USER ---");
        var result = await _registrationService.VerifyEmailAsync(request);

        // ============================================================
        // STEP 3: Verify rejection
        // ============================================================
        _assert.ShouldFail(result, expectedMessage: "User account not found.");
        _output.WriteLine("✓ Unknown user correctly rejected");
    }

    /// <summary>
    /// SCENARIO: Verify email after too many failed attempts
    /// EXPECTED: Verification fails with TooManyAttempts
    /// </summary>
    [Fact(DisplayName = "VerifyEmail - enforces attempt limit")]
    public async Task VerifyEmail_Enforces_Attempt_Limit()
    {
        // ============================================================
        // STEP 1: Setup unconfirmed user
        // ============================================================
        var testEmail = $"maxattempts_{Guid.NewGuid()}@test.com";
        var testUser = new ApplicationUser
        {
            UserName = testEmail,
            Email = testEmail,
            EmailConfirmed = false
        };

        await _userManager.CreateAsync(testUser, "TestPass123!");
        _output.WriteLine($"✓ Created unconfirmed user: {testEmail}");

        // ============================================================
        // STEP 2: Setup mock to return TooManyAttempts
        // ============================================================
        _mockOtpService
            .Setup(x => x.ValidateOtpAsync(testEmail, It.IsAny<string>()))
            .ReturnsAsync(OtpValidationResult.TooManyAttempts);

        // ============================================================
        // STEP 3: Attempt verification
        // ============================================================
        var request = new EmailVerificationRequestDto
        {
            Email = testEmail,
            VerificationCode = "123456"
        };

        _output.WriteLine("\n--- ATTEMPTING VERIFICATION AFTER MAX ATTEMPTS ---");
        var result = await _registrationService.VerifyEmailAsync(request);

        // ============================================================
        // STEP 4: Verify rate limiting
        // ============================================================
        _assert.ShouldFail(result, expectedMessage: "Too many attempts. Please wait before retrying.");
        _output.WriteLine("✓ Rate limit correctly enforced");

        // Cleanup
        var user = await _userManager.FindByEmailAsync(testEmail);
        await _userManager.DeleteAsync(user!);
    }

    #endregion

    #region Error Handling Tests

    /// <summary>
    /// SCENARIO: OTP generation fails during registration
    /// EXPECTED: Registration rolls back, user not created
    /// </summary>
    [Fact(DisplayName = "Register - handles OTP generation failure (REG_005)")]
    public async Task Register_Handles_OTP_Generation_Failure()
    {
        // ============================================================
        // STEP 1: Setup request
        // ============================================================
        var request = new RegisterRequestDto
        {
            Email = $"otpfail_{Guid.NewGuid()}@test.com",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!",
            FirstName = "Otp",
            LastName = "Fail"
        };

        // ============================================================
        // STEP 2: Setup mock to return invalid OTP
        // ============================================================
        _mockOtpService
            .Setup(x => x.GenerateOtpAsync(request.Email))
            .ReturnsAsync(""); // Empty OTP indicates failure

        // ============================================================
        // STEP 3: Execute registration
        // ============================================================
        _output.WriteLine("\n--- ATTEMPTING REGISTRATION WITH OTP FAILURE ---");
        var result = await _registrationService.RegisterAsync(request);

        // ============================================================
        // STEP 4: Verify failure
        // ============================================================
        _assert.ShouldFail(result, expectedMessage: "Failed to generate verification code. Please try again.");
        _output.WriteLine("✓ Registration failed due to OTP generation");

        // ============================================================
        // STEP 5: Verify rollback occurred
        // ============================================================
        var user = await _userManager.FindByEmailAsync(request.Email);
        Assert.Null(user);
        _output.WriteLine("✓ User creation rolled back successfully");

        // ============================================================
        // STEP 6: Verify error logged
        // ============================================================
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("REG_005")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
            Times.Once
        );
        _output.WriteLine("✓ Error logged with code REG_005");
    }

    /// <summary>
    /// SCENARIO: Role assignment fails during registration
    /// EXPECTED: User creation rolled back
    /// </summary>
    [Fact(DisplayName = "Register - handles role assignment failure (REG_004)")]
    public async Task Register_Handles_Role_Assignment_Failure()
    {
        // ============================================================
        // STEP 1: Setup request
        // ============================================================
        var request = new RegisterRequestDto
        {
            Email = $"rolefail_{Guid.NewGuid()}@test.com",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!",
            FirstName = "Role",
            LastName = "Fail"
        };

        // ============================================================
        // STEP 2: Setup OTP mock
        // ============================================================
        _mockOtpService
            .Setup(x => x.GenerateOtpAsync(It.IsAny<string>()))
            .ReturnsAsync("123456");

        // Note: Role assignment failure is harder to simulate without
        // manipulating the role store. This test documents expected behavior.
        // The service should delete the user if role assignment fails.
        
        _output.WriteLine("✓ Role assignment failure test documented");
        _output.WriteLine("  (Requires RoleManager mock for full implementation)");
    }

    #endregion

    #region Legacy Method Tests

    /// <summary>
    /// SCENARIO: Attempt to use deprecated ConfirmEmailAsync method
    /// EXPECTED: Returns failure indicating method is deprecated
    /// </summary>
    [Fact(DisplayName = "ConfirmEmail - returns deprecated warning (LEGACY_001)")]
    public async Task ConfirmEmail_Returns_Deprecated_Warning()
    {
        // ============================================================
        // STEP 1: Attempt to use legacy method
        // ============================================================
        _output.WriteLine("\n--- ATTEMPTING LEGACY CONFIRM EMAIL METHOD ---");
        
        #pragma warning disable CS0618 // Type or member is obsolete
        var result = await _registrationService.ConfirmEmailAsync("some_token", "test@example.com");
        #pragma warning restore CS0618

        // ============================================================
        // STEP 2: Verify deprecation message
        // ============================================================
        _assert.ShouldFail(result, expectedMessage: "Use VerifyEmailAsync with OTP codes.");
        _output.WriteLine("✓ Legacy method correctly returns deprecation message");

        // ============================================================
        // STEP 3: Verify warning logged
        // ============================================================
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("LEGACY_001")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
            Times.Once
        );
        _output.WriteLine("✓ Warning logged with code LEGACY_001");
    }

    #endregion

    #region Integration Tests

    /// <summary>
    /// SCENARIO: Complete registration and verification workflow
    /// EXPECTED: User goes from non-existent → unconfirmed → confirmed
    /// </summary>
    [Fact(DisplayName = "Integration - complete registration to verification flow")]
    public async Task Integration_Complete_Registration_To_Verification_Flow()
    {
        // ============================================================
        // STEP 1: Initial state - no user exists
        // ============================================================
        var testEmail = $"complete_{Guid.NewGuid()}@test.com";
        var initialUser = await _userManager.FindByEmailAsync(testEmail);
        Assert.Null(initialUser);
        _output.WriteLine($"✓ Initial state: No user exists for {testEmail}");

        // ============================================================
        // STEP 2: Register new user
        // ============================================================
        var generatedOtp = "123456";
        _mockOtpService
            .Setup(x => x.GenerateOtpAsync(testEmail))
            .ReturnsAsync(generatedOtp);

        var registerRequest = new RegisterRequestDto
        {
            Email = testEmail,
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!",
            FirstName = "Complete",
            LastName = "Flow"
        };

        _output.WriteLine("\n--- STEP 1: REGISTRATION ---");
        var registerResult = await _registrationService.RegisterAsync(registerRequest);
        
        _assert.ShouldSucceed(registerResult);
        Assert.False(registerResult.Data!.EmailVerified);
        _output.WriteLine("✓ Registration successful, user unconfirmed");

        // ============================================================
        // STEP 3: Verify user is unconfirmed
        // ============================================================
        var unconfirmedUser = await _userManager.FindByEmailAsync(testEmail);
        Assert.NotNull(unconfirmedUser);
        Assert.False(unconfirmedUser!.EmailConfirmed);
        _output.WriteLine("✓ User exists in database with EmailConfirmed = false");

        // ============================================================
        // STEP 4: Verify email with OTP
        // ============================================================
        _mockOtpService
            .Setup(x => x.ValidateOtpAsync(testEmail, generatedOtp))
            .ReturnsAsync(OtpValidationResult.Valid);

        var verifyRequest = new EmailVerificationRequestDto
        {
            Email = testEmail,
            VerificationCode = generatedOtp
        };

        _output.WriteLine("\n--- STEP 2: EMAIL VERIFICATION ---");
        var verifyResult = await _registrationService.VerifyEmailAsync(verifyRequest);
        
        _assert.ShouldSucceed(verifyResult);
        _output.WriteLine("✓ Email verification successful");

        // ============================================================
        // STEP 5: Verify user is now confirmed
        // ============================================================
        var confirmedUser = await _userManager.FindByEmailAsync(testEmail);
        Assert.NotNull(confirmedUser);
        Assert.True(confirmedUser!.EmailConfirmed);
        _output.WriteLine("✓ User EmailConfirmed = true");

        // ============================================================
        // STEP 6: Verify complete workflow logging
        // ============================================================
        _output.WriteLine("\n--- WORKFLOW SUMMARY ---");
        _output.WriteLine($"✓ User journey completed:");
        _output.WriteLine($"  1. Registration → User created (unconfirmed)");
        _output.WriteLine($"  2. OTP generated → {generatedOtp}");
        _output.WriteLine($"  3. Email sent → Hangfire job enqueued");
        _output.WriteLine($"  4. Verification → OTP validated");
        _output.WriteLine($"  5. Activation → Email confirmed");
        _output.WriteLine($"  6. Welcome → Welcome email enqueued");

        // Cleanup
        await _userManager.DeleteAsync(confirmedUser);
    }

    /// <summary>
    /// SCENARIO: Concurrent registrations for different users
    /// EXPECTED: All registrations succeed independently
    /// </summary>
    [Fact(DisplayName = "Integration - handles concurrent registrations")]
    public async Task Integration_Handles_Concurrent_Registrations()
    {
        // ============================================================
        // STEP 1: Setup multiple registration requests
        // ============================================================
        var requests = Enumerable.Range(1, 5).Select(i => new RegisterRequestDto
        {
            Email = $"concurrent{i}_{Guid.NewGuid()}@test.com",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!",
            FirstName = $"User{i}",
            LastName = "Concurrent"
        }).ToList();

        _output.WriteLine($"✓ Prepared {requests.Count} concurrent registration requests");

        // ============================================================
        // STEP 2: Setup mocks for all users
        // ============================================================
        foreach (var request in requests)
        {
            _mockOtpService
                .Setup(x => x.GenerateOtpAsync(request.Email))
                .ReturnsAsync($"{new Random().Next(100000, 999999)}");
        }

        // ============================================================
        // STEP 3: Execute registrations concurrently
        // ============================================================
        _output.WriteLine("\n--- EXECUTING CONCURRENT REGISTRATIONS ---");
        var tasks = requests.Select(req => _registrationService.RegisterAsync(req));
        var results = await Task.WhenAll(tasks);

        // ============================================================
        // STEP 4: Verify all succeeded
        // ============================================================
        Assert.All(results, result => _assert.ShouldSucceed(result));
        _output.WriteLine($"✓ All {results.Length} registrations succeeded");

        // ============================================================
        // STEP 5: Verify all users created
        // ============================================================
        foreach (var request in requests)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            Assert.NotNull(user);
            Assert.False(user!.EmailConfirmed);
            _output.WriteLine($"✓ User created: {request.Email}");
        }

        // ============================================================
        // STEP 6: Verify OTP generated for each
        // ============================================================
        _mockOtpService.Verify(
            x => x.GenerateOtpAsync(It.IsAny<string>()),
            Times.Exactly(requests.Count)
        );
        _output.WriteLine($"✓ OTP generated for all {requests.Count} users");

        // Cleanup
        foreach (var request in requests)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user != null) await _userManager.DeleteAsync(user);
        }
    }

    #endregion

    #region Cleanup
    
    public new void Dispose()
    {
        // Clean up resources if necessary
        base.Dispose();
    }
    #endregion
}*/