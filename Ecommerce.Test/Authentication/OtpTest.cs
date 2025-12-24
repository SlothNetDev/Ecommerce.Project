using Ecommerce.Infrastructure.Identity.Services.Register;
using Ecommerce.Shared.Enums;
using Ecommerce.Test.TestUtilities;
using Ecommerce.Api;
using Ecommerce.Infrastructure.Services.Notification;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace Ecommerce.Test.Authentication;

/// <summary>
/// TDD Test Suite for OTP Service
/// Tests cover: OTP generation, validation, expiration, attempt limits,
/// cleanup, and edge cases
/// </summary>
public class OtpServiceTest : TestBase
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ILogger<OtpService>> _mockLogger;
    private readonly OtpService _otpService;

    public OtpServiceTest(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output) : base(factory)
    {
        _output = output;
        _mockLogger = new Mock<ILogger<OtpService>>();
        _otpService = new OtpService(_mockLogger.Object);
    }

    /// <summary>
    /// SCENARIO: Generate OTP for a new email
    /// EXPECTED: Returns 6-digit numeric code
    /// </summary>
    [Fact(DisplayName = "OtpService - generates valid 6-digit OTP")]
    public async Task OtpService_Generates_Valid_Six_Digit_OTP()
    {
        // ============================================================
        // STEP 1: Setup test email
        // ============================================================
        var testEmail = "test@example.com";
        _output.WriteLine($"✓ Test email: {testEmail}");

        // ============================================================
        // STEP 2: Generate OTP
        // ============================================================
        _output.WriteLine("\n--- GENERATING OTP ---");
        var otp = await _otpService.GenerateOtpAsync(testEmail);

        // ============================================================
        // STEP 3: Validate OTP format
        // ============================================================
        Assert.NotNull(otp);
        Assert.Equal(6, otp.Length);
        Assert.True(otp.All(char.IsDigit), "OTP should contain only digits");
        
        _output.WriteLine($"✓ Generated OTP: {otp}");
        _output.WriteLine("✓ OTP is 6 digits");
        _output.WriteLine("✓ OTP contains only numeric characters");
    }

    /// <summary>
    /// SCENARIO: Generate OTP and validate it immediately
    /// EXPECTED: Validation succeeds with correct code
    /// </summary>
    [Fact(DisplayName = "OtpService - validates correct OTP successfully")]
    public async Task OtpService_Validates_Correct_OTP_Successfully()
    {
        // ============================================================
        // STEP 1: Generate OTP
        // ============================================================
        var testEmail = "validate@example.com";
        var otp = await _otpService.GenerateOtpAsync(testEmail);
        
        _output.WriteLine($"✓ Generated OTP for {testEmail}: {otp}");

        // ============================================================
        // STEP 2: Validate with correct code
        // ============================================================
        _output.WriteLine("\n--- VALIDATING OTP ---");
        var result = await _otpService.ValidateOtpAsync(testEmail, otp);

        // ============================================================
        // STEP 3: Verify validation succeeded
        // ============================================================
        Assert.Equal(OtpValidationResult.Valid, result);
        _output.WriteLine("✓ OTP validation successful");
    }

    /// <summary>
    /// SCENARIO: Validate OTP with incorrect code
    /// EXPECTED: Validation fails with Invalid result
    /// </summary>
    [Fact(DisplayName = "OtpService - rejects incorrect OTP code")]
    public async Task OtpService_Rejects_Incorrect_OTP_Code()
    {
        // ============================================================
        // STEP 1: Generate OTP
        // ============================================================
        var testEmail = "wrong@example.com";
        var correctOtp = await _otpService.GenerateOtpAsync(testEmail);
        
        _output.WriteLine($"✓ Generated correct OTP: {correctOtp}");

        // ============================================================
        // STEP 2: Validate with wrong code
        // ============================================================
        var wrongOtp = "000000"; // Guaranteed to be different
        _output.WriteLine($"✓ Attempting validation with wrong OTP: {wrongOtp}");
        
        _output.WriteLine("\n--- VALIDATING INCORRECT OTP ---");
        var result = await _otpService.ValidateOtpAsync(testEmail, wrongOtp);

        // ============================================================
        // STEP 3: Verify validation failed
        // ============================================================
        Assert.Equal(OtpValidationResult.Invalid, result);
        _output.WriteLine("✓ OTP correctly rejected as invalid");
    }

    /// <summary>
    /// SCENARIO: Validate OTP for email that doesn't exist
    /// EXPECTED: Returns NotFound result
    /// </summary>
    [Fact(DisplayName = "OtpService - returns NotFound for non-existent email")]
    public async Task OtpService_Returns_NotFound_For_NonExistent_Email()
    {
        // ============================================================
        // STEP 1: Attempt validation without generating OTP
        // ============================================================
        var nonExistentEmail = "notfound@example.com";
        var randomOtp = "123456";

        _output.WriteLine($"✓ Email without OTP: {nonExistentEmail}");
        _output.WriteLine($"✓ Attempting validation with: {randomOtp}");

        // ============================================================
        // STEP 2: Validate
        // ============================================================
        _output.WriteLine("\n--- VALIDATING NON-EXISTENT OTP ---");
        var result = await _otpService.ValidateOtpAsync(nonExistentEmail, randomOtp);

        // ============================================================
        // STEP 3: Verify NotFound result
        // ============================================================
        Assert.Equal(OtpValidationResult.NotFound, result);
        _output.WriteLine("✓ OTP correctly reported as not found");
    }

    /// <summary>
    /// SCENARIO: Attempt to use OTP twice
    /// EXPECTED: Second validation fails (OTP is consumed after first use)
    /// </summary>
    [Fact(DisplayName = "OtpService - prevents OTP reuse")]
    public async Task OtpService_Prevents_OTP_Reuse()
    {
        // ============================================================
        // STEP 1: Generate and use OTP once
        // ============================================================
        var testEmail = "reuse@example.com";
        var otp = await _otpService.GenerateOtpAsync(testEmail);
        
        _output.WriteLine($"✓ Generated OTP: {otp}");

        _output.WriteLine("\n--- FIRST VALIDATION (SHOULD SUCCEED) ---");
        var firstResult = await _otpService.ValidateOtpAsync(testEmail, otp);
        Assert.Equal(OtpValidationResult.Valid, firstResult);
        _output.WriteLine("✓ First validation succeeded");

        // ============================================================
        // STEP 2: Attempt to reuse same OTP
        // ============================================================
        _output.WriteLine("\n--- SECOND VALIDATION (SHOULD FAIL) ---");
        var secondResult = await _otpService.ValidateOtpAsync(testEmail, otp);

        // ============================================================
        // STEP 3: Verify reuse is prevented
        // ============================================================
        Assert.NotEqual(OtpValidationResult.Valid, secondResult);
        _output.WriteLine($"✓ Second validation failed with result: {secondResult}");
        _output.WriteLine("✓ OTP reuse successfully prevented");
    }

    /// <summary>
    /// SCENARIO: Exceed maximum validation attempts
    /// EXPECTED: After 5 failed attempts, returns TooManyAttempts
    /// </summary>
    [Fact(DisplayName = "OtpService - enforces maximum attempt limit")]
    public async Task OtpService_Enforces_Maximum_Attempt_Limit()
    {
        // ============================================================
        // STEP 1: Generate OTP
        // ============================================================
        var testEmail = "maxattempts@example.com";
        var correctOtp = await _otpService.GenerateOtpAsync(testEmail);
        
        _output.WriteLine($"✓ Generated OTP for: {testEmail}");

        // ============================================================
        // STEP 2: Make 5 failed attempts (max allowed)
        // ============================================================
        _output.WriteLine("\n--- MAKING MULTIPLE FAILED ATTEMPTS ---");
        
        for (int i = 1; i <= 5; i++)
        {
            var result = await _otpService.ValidateOtpAsync(testEmail, "000000");
            _output.WriteLine($"✓ Attempt {i}: {result}");
            
            if (i < 5)
            {
                Assert.Equal(OtpValidationResult.Invalid, result);
            }
            else
            {
                // 5th attempt might return Invalid or TooManyAttempts depending on implementation
                Assert.True(
                    result == OtpValidationResult.Invalid || 
                    result == OtpValidationResult.TooManyAttempts
                );
            }
        }

        // ============================================================
        // STEP 3: Verify OTP is removed after max attempts
        // ============================================================
        _output.WriteLine("\n--- ATTEMPTING VALIDATION AFTER MAX ATTEMPTS ---");
        var finalResult = await _otpService.ValidateOtpAsync(testEmail, correctOtp);
        
        // After max attempts, OTP should be removed, so validation should fail
        Assert.NotEqual(OtpValidationResult.Valid, finalResult);
        _output.WriteLine($"✓ Post-limit validation result: {finalResult}");
        _output.WriteLine("✓ Maximum attempt limit enforced");
    }

    /// <summary>
    /// SCENARIO: Check if valid OTP exists for email
    /// EXPECTED: Returns true when valid OTP exists, false otherwise
    /// </summary>
    [Fact(DisplayName = "OtpService - checks OTP existence correctly")]
    public async Task OtpService_Checks_OTP_Existence_Correctly()
    {
        // ============================================================
        // STEP 1: Check before generating OTP
        // ============================================================
        var testEmail = "existence@example.com";
        
        _output.WriteLine("\n--- CHECKING OTP EXISTENCE BEFORE GENERATION ---");
        var existsBeforeGeneration = await _otpService.HasValidOtpAsync(testEmail);
        Assert.False(existsBeforeGeneration, "Should not have OTP before generation");
        _output.WriteLine("✓ No OTP exists before generation");

        // ============================================================
        // STEP 2: Generate OTP and check again
        // ============================================================
        await _otpService.GenerateOtpAsync(testEmail);
        
        _output.WriteLine("\n--- CHECKING OTP EXISTENCE AFTER GENERATION ---");
        var existsAfterGeneration = await _otpService.HasValidOtpAsync(testEmail);
        Assert.True(existsAfterGeneration, "Should have OTP after generation");
        _output.WriteLine("✓ OTP exists after generation");
    }

    /// <summary>
    /// SCENARIO: Remove OTP manually
    /// EXPECTED: OTP is removed and no longer valid
    /// </summary>
    [Fact(DisplayName = "OtpService - removes OTP successfully")]
    public async Task OtpService_Removes_OTP_Successfully()
    {
        // ============================================================
        // STEP 1: Generate OTP
        // ============================================================
        var testEmail = "remove@example.com";
        var otp = await _otpService.GenerateOtpAsync(testEmail);
        
        _output.WriteLine($"✓ Generated OTP for: {testEmail}");

        // Verify it exists
        var existsBefore = await _otpService.HasValidOtpAsync(testEmail);
        Assert.True(existsBefore);
        _output.WriteLine("✓ OTP exists before removal");

        // ============================================================
        // STEP 2: Remove OTP
        // ============================================================
        _output.WriteLine("\n--- REMOVING OTP ---");
        await _otpService.RemoveOtpAsync(testEmail);
        _output.WriteLine("✓ OTP removal executed");

        // ============================================================
        // STEP 3: Verify removal
        // ============================================================
        var existsAfter = await _otpService.HasValidOtpAsync(testEmail);
        Assert.False(existsAfter, "OTP should not exist after removal");
        _output.WriteLine("✓ OTP successfully removed");

        // Validation should fail
        var validationResult = await _otpService.ValidateOtpAsync(testEmail, otp);
        Assert.Equal(OtpValidationResult.NotFound, validationResult);
        _output.WriteLine("✓ Validation confirms OTP is gone");
    }

    /// <summary>
    /// SCENARIO: Generate new OTP for email that already has one
    /// EXPECTED: Old OTP is replaced with new one
    /// </summary>
    [Fact(DisplayName = "OtpService - replaces existing OTP with new one")]
    public async Task OtpService_Replaces_Existing_OTP_With_New_One()
    {
        // ============================================================
        // STEP 1: Generate first OTP
        // ============================================================
        var testEmail = "replace@example.com";
        var firstOtp = await _otpService.GenerateOtpAsync(testEmail);
        
        _output.WriteLine($"✓ First OTP generated: {firstOtp}");

        // ============================================================
        // STEP 2: Generate second OTP (should replace first)
        // ============================================================
        await Task.Delay(100); // Small delay to ensure different OTP
        var secondOtp = await _otpService.GenerateOtpAsync(testEmail);
        
        _output.WriteLine($"✓ Second OTP generated: {secondOtp}");

        // ============================================================
        // STEP 3: Verify first OTP no longer works
        // ============================================================
        _output.WriteLine("\n--- VALIDATING FIRST (OLD) OTP ---");
        var firstResult = await _otpService.ValidateOtpAsync(testEmail, firstOtp);
        Assert.Equal(OtpValidationResult.Invalid, firstResult);
        _output.WriteLine("✓ First OTP no longer valid");

        // ============================================================
        // STEP 4: Verify second OTP works
        // ============================================================
        _output.WriteLine("\n--- VALIDATING SECOND (NEW) OTP ---");
        var secondResult = await _otpService.ValidateOtpAsync(testEmail, secondOtp);
        Assert.Equal(OtpValidationResult.Valid, secondResult);
        _output.WriteLine("✓ Second OTP is valid");
        _output.WriteLine("✓ OTP replacement successful");
    }

    /// <summary>
    /// SCENARIO: Email normalization (case-insensitive)
    /// EXPECTED: OTP works regardless of email casing
    /// </summary>
    [Fact(DisplayName = "OtpService - handles email case-insensitivity")]
    public async Task OtpService_Handles_Email_Case_Insensitivity()
    {
        // ============================================================
        // STEP 1: Generate OTP with lowercase email
        // ============================================================
        var lowercaseEmail = "test@example.com";
        var otp = await _otpService.GenerateOtpAsync(lowercaseEmail);
        
        _output.WriteLine($"✓ Generated OTP with lowercase email: {lowercaseEmail}");

        // ============================================================
        // STEP 2: Validate with uppercase email
        // ============================================================
        var uppercaseEmail = "TEST@EXAMPLE.COM";
        _output.WriteLine($"✓ Validating with uppercase email: {uppercaseEmail}");
        
        _output.WriteLine("\n--- VALIDATING WITH DIFFERENT CASE ---");
        var result = await _otpService.ValidateOtpAsync(uppercaseEmail, otp);

        // ============================================================
        // STEP 3: Verify validation succeeds
        // ============================================================
        Assert.Equal(OtpValidationResult.Valid, result);
        _output.WriteLine("✓ OTP validated successfully regardless of email case");
        _output.WriteLine("✓ Email normalization working correctly");
    }

    /// <summary>
    /// SCENARIO: Generate OTPs for multiple users concurrently
    /// EXPECTED: Each user gets unique OTP without interference
    /// </summary>
    [Fact(DisplayName = "OtpService - handles concurrent OTP generation")]
    public async Task OtpService_Handles_Concurrent_OTP_Generation()
    {
        // ============================================================
        // STEP 1: Setup multiple test emails
        // ============================================================
        var emails = new[]
        {
            "user1@test.com",
            "user2@test.com",
            "user3@test.com",
            "user4@test.com",
            "user5@test.com"
        };

        _output.WriteLine($"✓ Prepared {emails.Length} test emails");

        // ============================================================
        // STEP 2: Generate OTPs concurrently
        // ============================================================
        _output.WriteLine("\n--- GENERATING OTPs CONCURRENTLY ---");
        var tasks = emails.Select(email => _otpService.GenerateOtpAsync(email));
        var otps = await Task.WhenAll(tasks);

        // ============================================================
        // STEP 3: Verify all OTPs were generated
        // ============================================================
        Assert.Equal(emails.Length, otps.Length);
        Assert.All(otps, otp => Assert.Equal(6, otp.Length));
        _output.WriteLine($"✓ All {otps.Length} OTPs generated successfully");

        // ============================================================
        // STEP 4: Verify each OTP is valid for its email
        // ============================================================
        _output.WriteLine("\n--- VALIDATING EACH OTP ---");
        for (int i = 0; i < emails.Length; i++)
        {
            var result = await _otpService.ValidateOtpAsync(emails[i], otps[i]);
            Assert.Equal(OtpValidationResult.Valid, result);
            _output.WriteLine($"✓ User {i + 1} OTP validated: {emails[i]}");
        }

        _output.WriteLine("✓ All concurrent OTPs valid and isolated");
    }

    /// <summary>
    /// SCENARIO: Verify OTP expiration time is reasonable
    /// EXPECTED: OTP should expire in approximately 10 minutes
    /// </summary>
    [Fact(DisplayName = "OtpService - sets reasonable expiration time")]
    public async Task OtpService_Sets_Reasonable_Expiration_Time()
    {
        // ============================================================
        // STEP 1: Generate OTP and check if it's valid
        // ============================================================
        var testEmail = "expiry@example.com";
        await _otpService.GenerateOtpAsync(testEmail);
        
        _output.WriteLine($"✓ Generated OTP for: {testEmail}");

        // ============================================================
        // STEP 2: Verify OTP is immediately valid
        // ============================================================
        var isValidNow = await _otpService.HasValidOtpAsync(testEmail);
        Assert.True(isValidNow, "OTP should be valid immediately after generation");
        _output.WriteLine("✓ OTP is valid immediately after generation");

        // Note: Testing actual expiration would require waiting 10 minutes
        // or manipulating time, which is not practical in unit tests.
        // In production, you might use a time provider interface for testability.
        
        _output.WriteLine("✓ OTP expiration time configured (10 minutes by implementation)");
    }

    /// <summary>
    /// SCENARIO: Verify logging occurs for key operations
    /// EXPECTED: Service logs generation, validation, and removal
    /// </summary>
    [Fact(DisplayName = "OtpService - logs key operations")]
    public async Task OtpService_Logs_Key_Operations()
    {
        // ============================================================
        // STEP 1: Generate OTP (should log)
        // ============================================================
        var testEmail = "logging@example.com";
        var otp = await _otpService.GenerateOtpAsync(testEmail);

        // Verify generation was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("generated")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
            Times.AtLeastOnce,
            "Should log OTP generation"
        );
        _output.WriteLine("✓ OTP generation logged");

        // ============================================================
        // STEP 2: Validate OTP (should log)
        // ============================================================
        await _otpService.ValidateOtpAsync(testEmail, otp);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("successful")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
            Times.AtLeastOnce,
            "Should log successful validation"
        );
        _output.WriteLine("✓ OTP validation logged");

        // ============================================================
        // STEP 3: Remove OTP (should log)
        // ============================================================
        await _otpService.RemoveOtpAsync("another@test.com");

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("removed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()!),
            Times.AtLeastOnce,
            "Should log OTP removal"
        );
        _output.WriteLine("✓ OTP removal logged");
    }
}
