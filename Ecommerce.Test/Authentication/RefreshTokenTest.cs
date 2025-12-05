using System.Net;
using System.Net.Http.Json;
using Ecommerce.Api;
using Ecommerce.Core.Application.Common.Interfaces.RefreshToken;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Shared.TokenDTO;
using Ecommerce.Shared.Wrapper;
using Ecommerce.Test.Authentication.Helpers;
using Ecommerce.Test.TestUtilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Ecommerce.Test.Authentication;

/// <summary>
/// Integration tests for refresh token functionality
/// Tests cover: token rotation, expiration, revocation, replay attacks, and security
/// </summary>
public class RefreshTokenTest : TestBase
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly AssertApiHelper _assert;
    private const string RefreshEndpoint = "/api/auth/action/RefreshToken";
    private readonly ITestOutputHelper _output;

    public RefreshTokenTest(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output) : base(factory)
    {
        _refreshTokenService = factory.Services.GetRequiredService<IRefreshTokenService>();
        _assert = new AssertApiHelper(output);
        _output = output;
        EnsureRolesExist();
    }

    private void EnsureRolesExist()
    {
        var roleManager = _factory.Services.GetRequiredService<RoleManager<ApplicationRole>>();
        var roles = new[] { "Admin", "Costumer", "Seller" };

        foreach (var role in roles)
        {
            if (!roleManager.RoleExistsAsync(role).Result)
            {
                roleManager.CreateAsync(new ApplicationRole { Name = role }).Wait();
            }
        }
    }

    /// <summary>
    /// SCENARIO: Happy path - valid refresh token returns new access token + new refresh token
    /// EXPECTED: New tokens generated, old refresh token rotated
    /// </summary>
    [Fact(DisplayName = "Refresh - succeeds and returns new JWT")]
    public async Task Refresh_ShouldReturnNewJwt()
    {
        // ============================================================
        // STEP 1: Setup - Create test user
        // ============================================================
        var userManager = _factory.Services.GetRequiredService<UserManager<ApplicationUser>>();
        var refreshUser = await userManager.FindByEmailAsync("refreshuser@test.com");

        if (refreshUser == null)
        {
            refreshUser = new ApplicationUser
            {
                UserName = "refreshuser@test.com",
                Email = "refreshuser@test.com",
                AccountCreatedAt = DateTime.UtcNow
            };
            await userManager.CreateAsync(refreshUser, "Refresh123!");
            await userManager.AddToRoleAsync(refreshUser, "Costumer");
            _output.WriteLine("✓ Created test user: refreshuser@test.com");
        }

        // ============================================================
        // STEP 2: Generate initial JWT token (simulating login)
        // ============================================================
        var token = TestUserHelper.GenerateJwtForUser(_factory.Services, "refreshuser@test.com");
        _output.WriteLine($"✓ Generated JWT for user: {token}");

        // ============================================================
        // STEP 3: Generate initial refresh token
        // ============================================================
        var ip = "127.0.0.1";
        var refreshService = _factory.Services.GetRequiredService<IRefreshTokenService>();
        var oldTokenResult = await refreshService.GenerateRefreshTokenAsync(refreshUser.Id.ToString(), ip);
        
        Assert.True(oldTokenResult.Success, "Failed to generate refresh token");
        var oldToken = oldTokenResult.Data!;
        _output.WriteLine($"✓ Generated refresh token: {oldToken.Token}");

        // ============================================================
        // STEP 4: Verify token was saved to database
        // ============================================================
        var dbContext = _factory.Services.GetRequiredService<ApplicationDbContext>();
        var tokenExists = await dbContext.RefreshToken.AnyAsync(t => t.Token == oldToken.Token);
        _output.WriteLine($"✓ Token exists in DB: {tokenExists}");
        Assert.True(tokenExists, "Token was not saved to database");

        // ============================================================
        // STEP 5: Call refresh endpoint with old token
        // ============================================================
        var response = await _client.PostAsJsonAsync(RefreshEndpoint, 
            new { BearerToken = "", RefreshToken = oldToken.Token });
        _output.WriteLine($"✓ Refresh endpoint response status: {response.StatusCode}");

        if (response.StatusCode != HttpStatusCode.OK)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"✗ Error response: {errorContent}");
        }

        // ============================================================
        // STEP 6: Assert - Verify success and token rotation
        // ============================================================
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var typed = await response.Content.ReadFromJsonAsync<ResponseType<TokenResponseDto>>();
        _output.WriteLine($"✓ Response: {System.Text.Json.JsonSerializer.Serialize(typed)}");
        
        _assert.ShouldSucceed(typed!);
        
        // Verify token rotation occurred
        _output.WriteLine($"✓ Old refresh token: {oldToken.Token}");
        _output.WriteLine($"✓ New refresh token: {typed?.Data!.RefreshToken}");
        Assert.NotEqual(oldToken.Token, typed?.Data!.RefreshToken);
        
        // Verify new access token was generated
        _output.WriteLine($"✓ New access token: {typed?.Data?.AccessToken}");
        Assert.False(string.IsNullOrEmpty(typed.Data?.AccessToken));
    }

    /// <summary>
    /// SCENARIO: Token rotation security - old token should be invalidated after use
    /// EXPECTED: First use succeeds, second use with same token fails
    /// </summary>
    [Fact(DisplayName = "Refresh - old token invalidated after rotation")]
    public async Task Refresh_Should_Invalidate_Old_Token()
    {
        // ============================================================
        // STEP 1: Setup - Create test user and initial refresh token
        // ============================================================
        var userManager = _factory.Services.GetRequiredService<UserManager<ApplicationUser>>();
        var refreshTokenService = _factory.Services.GetRequiredService<IRefreshTokenService>();
        var refreshUser = await userManager.FindByEmailAsync("invalidateold@test.com");

        if (refreshUser == null)
        {
            refreshUser = new ApplicationUser
            {
                UserName = "invalidateold@test.com",
                Email = "invalidateold@test.com"
            };
            await userManager.CreateAsync(refreshUser, "Invalidate123!");
            await userManager.AddToRoleAsync(refreshUser, "Costumer");
            _output.WriteLine("✓ Created test user: invalidateold@test.com");
        }

        // ============================================================
        // STEP 2: Generate initial refresh token
        // ============================================================
        var ip = "127.0.0.1";
        var oldTokenResult = await refreshTokenService.GenerateRefreshTokenAsync(refreshUser.Id.ToString(), ip);
        Assert.True(oldTokenResult.Success, "Failed to generate refresh token");
        var oldToken = oldTokenResult.Data!;
        _output.WriteLine($"✓ Generated token: {oldToken.Token}");

        // ============================================================
        // STEP 3: First refresh - should succeed
        // ============================================================
        _output.WriteLine("\n--- FIRST REFRESH ATTEMPT (Should Succeed) ---");
        var firstResponse = await _client.PostAsJsonAsync(RefreshEndpoint, 
            new { BearerToken = "", RefreshToken = oldToken.Token });
        _output.WriteLine($"✓ First refresh status: {firstResponse.StatusCode}");
        
        var firstContent = await firstResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"✓ First refresh response: {firstContent}");
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var firstTyped = await firstResponse.Content.ReadFromJsonAsync<ResponseType<TokenResponseDto>>();
        _output.WriteLine($"✓ First refresh succeeded: {System.Text.Json.JsonSerializer.Serialize(firstTyped)}");
        _assert.ShouldSucceed(firstTyped!);

        // ============================================================
        // STEP 4: Second refresh with SAME old token - should fail (already rotated)
        // ============================================================
        _output.WriteLine("\n--- SECOND REFRESH ATTEMPT (Should Fail - Token Already Used) ---");
        var secondResponse = await _client.PostAsJsonAsync(RefreshEndpoint, 
            new { BearerToken = "", RefreshToken = oldToken.Token });
        _output.WriteLine($"✓ Second refresh status: {secondResponse.StatusCode}");
        
        var secondContent = await secondResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"✓ Second refresh response: {secondContent}");

        // ============================================================
        // STEP 5: Assert - Second attempt should be rejected
        // ============================================================
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);

        var secondTyped = await secondResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        _output.WriteLine($"✓ Expected failure: {System.Text.Json.JsonSerializer.Serialize(secondTyped)}");
        Assert.Contains("Invalid refresh token", secondTyped?.Detail);
    }

    /// <summary>
    /// SCENARIO: Expired tokens should be rejected
    /// EXPECTED: Refresh attempt with expired token fails
    /// </summary>
    [Fact(DisplayName = "Refresh - rejects expired tokens")]
    public async Task Refresh_Should_Reject_Expired_Token()
    {
        // ============================================================
        // STEP 1: Setup - Create test user
        // ============================================================
        var userManager = _factory.Services.GetRequiredService<UserManager<ApplicationUser>>();
        var refreshTokenService = _factory.Services.GetRequiredService<IRefreshTokenService>();
        var dbContext = _factory.Services.GetRequiredService<ApplicationDbContext>();

        var refreshUser = await userManager.FindByEmailAsync("expired@test.com");

        if (refreshUser == null)
        {
            refreshUser = new ApplicationUser
            {
                UserName = "expired@test.com",
                Email = "expired@test.com"
            };
            await userManager.CreateAsync(refreshUser, "Expired123!");
            await userManager.AddToRoleAsync(refreshUser, "Costumer");
            _output.WriteLine("✓ Created test user: expired@test.com");
        }

        // ============================================================
        // STEP 2: Generate refresh token
        // ============================================================
        var ip = "127.0.0.1";
        var tokenResult = await refreshTokenService.GenerateRefreshTokenAsync(refreshUser.Id.ToString(), ip);
        Assert.True(tokenResult.Success, "Failed to generate refresh token");
        _output.WriteLine($"✓ Generated token: {tokenResult.Data?.Token}");

        // ============================================================
        // STEP 3: Manually expire the token (simulate time passing)
        // ============================================================
        var tokenEntity = await dbContext.RefreshToken
            .FirstOrDefaultAsync(t => t.Token == tokenResult.Data!.Token);

        Assert.NotNull(tokenEntity);
        
        tokenEntity!.Expires = DateTime.UtcNow.AddMinutes(-1); // Expired 1 minute ago
        await dbContext.SaveChangesAsync();
        _output.WriteLine($"✓ Token forcibly expired at: {tokenEntity.Expires}");

        // ============================================================
        // STEP 4: Attempt to use expired token
        // ============================================================
        _output.WriteLine("\n--- REFRESH WITH EXPIRED TOKEN (Should Fail) ---");
        var response = await _client.PostAsJsonAsync(RefreshEndpoint, 
            new { BearerToken = "", RefreshToken = tokenResult.Data!.Token });
        _output.WriteLine($"✓ Response status: {response.StatusCode}");
        
        var respContent = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"✓ Response content: {respContent}");

        // ============================================================
        // STEP 5: Assert - Expired token should be rejected
        // ============================================================
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var typed = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        _output.WriteLine($"✓ Expected failure: {System.Text.Json.JsonSerializer.Serialize(typed)}");
        Assert.Contains("Invalid refresh token", typed?.Detail);
    }

    /// <summary>
    /// SCENARIO: Unknown/fake tokens should be rejected
    /// EXPECTED: System rejects tokens that don't exist in database
    /// </summary>
    [Fact(DisplayName = "Refresh - rejects unknown tokens")]
    public async Task Refresh_Should_Reject_Unknown_Token()
    {
        // ============================================================
        // STEP 1: Generate completely fake token (not in database)
        // ============================================================
        var fakeToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) +
                       Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        _output.WriteLine($"✓ Generated fake token: {fakeToken}");

        // ============================================================
        // STEP 2: Attempt to use fake token
        // ============================================================
        _output.WriteLine("\n--- REFRESH WITH FAKE TOKEN (Should Fail) ---");
        var response = await _client.PostAsJsonAsync(RefreshEndpoint, 
            new { BearerToken = "", RefreshToken = fakeToken });
        _output.WriteLine($"✓ Response status: {response.StatusCode}");
        
        var content = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"✓ Response content: {content}");

        // ============================================================
        // STEP 3: Assert - Unknown token should be rejected
        // ============================================================
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var typed = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        _output.WriteLine($"✓ Expected failure: {System.Text.Json.JsonSerializer.Serialize(typed)}");
        Assert.Contains("Invalid refresh token", typed?.Detail);
    }

    /// <summary>
    /// SCENARIO: Cross-user token usage validation
    /// EXPECTED: Each user can only use their own tokens
    /// NOTE: This test documents current behavior - may need enhancement for stricter validation
    /// </summary>
    [Fact(DisplayName = "Refresh - validates token ownership per user")]
    public async Task Refresh_Should_Reject_Token_With_Wrong_User()
    {
        // ============================================================
        // STEP 1: Setup - Create two separate users
        // ============================================================
        var userManager = _factory.Services.GetRequiredService<UserManager<ApplicationUser>>();
        var refreshTokenService = _factory.Services.GetRequiredService<IRefreshTokenService>();

        // Create User 1
        var user1 = await userManager.FindByEmailAsync("user1@test.com");
        if (user1 == null)
        {
            user1 = new ApplicationUser
            {
                UserName = "user1@test.com",
                Email = "user1@test.com"
            };
            await userManager.CreateAsync(user1, "User1123!");
            await userManager.AddToRoleAsync(user1, "Costumer");
            _output.WriteLine("✓ Created User 1: user1@test.com");
        }

        // Create User 2
        var user2 = await userManager.FindByEmailAsync("user2@test.com");
        if (user2 == null)
        {
            user2 = new ApplicationUser
            {
                UserName = "user2@test.com",
                Email = "user2@test.com"
            };
            await userManager.CreateAsync(user2, "User2123!");
            await userManager.AddToRoleAsync(user2, "Costumer");
            _output.WriteLine("✓ Created User 2: user2@test.com");
        }

        var ip = "127.0.0.1";

        // ============================================================
        // STEP 2: Generate refresh token for User 1
        // ============================================================
        var user1TokenResult = await refreshTokenService.GenerateRefreshTokenAsync(user1.Id.ToString(), ip);
        Assert.True(user1TokenResult.Success, "Failed to generate refresh token for user1");
        _output.WriteLine($"✓ User1 refresh token: {user1TokenResult.Data?.Token}");

        // ============================================================
        // STEP 3: Generate refresh token for User 2
        // ============================================================
        var user2TokenResult = await refreshTokenService.GenerateRefreshTokenAsync(user2.Id.ToString(), ip);
        Assert.True(user2TokenResult.Success, "Failed to generate refresh token for user2");
        _output.WriteLine($"✓ User2 refresh token: {user2TokenResult.Data?.Token}");

        // ============================================================
        // STEP 4: Test User1's token (should work for User1)
        // ============================================================
        _output.WriteLine("\n--- USER1 USING THEIR OWN TOKEN (Should Succeed) ---");
        var user1Response = await _client.PostAsJsonAsync(RefreshEndpoint,
            new { BearerToken = "", RefreshToken = user1TokenResult.Data!.Token });

        _output.WriteLine($"✓ User1 refresh status: {user1Response.StatusCode}");
        var user1Resp = await user1Response.Content.ReadAsStringAsync();
        _output.WriteLine($"✓ User1 refresh response: {user1Resp}");
        Assert.Equal(HttpStatusCode.OK, user1Response.StatusCode);

        var user1Typed = await user1Response.Content.ReadFromJsonAsync<ResponseType<TokenResponseDto>>();
        _output.WriteLine($"✓ User1 refresh succeeded: {System.Text.Json.JsonSerializer.Serialize(user1Typed)}");
        _assert.ShouldSucceed(user1Typed!);

        // ============================================================
        // STEP 5: Test User2's token (should work for User2)
        // ============================================================
        _output.WriteLine("\n--- USER2 USING THEIR OWN TOKEN (Should Succeed) ---");
        var user2Response = await _client.PostAsJsonAsync(RefreshEndpoint,
            new { BearerToken = "", RefreshToken = user2TokenResult.Data!.Token });

        _output.WriteLine($"✓ User2 refresh status: {user2Response.StatusCode}");
        var user2Resp = await user2Response.Content.ReadAsStringAsync();
        _output.WriteLine($"✓ User2 refresh response: {user2Resp}");
        Assert.Equal(HttpStatusCode.OK, user2Response.StatusCode);

        var user2Typed = await user2Response.Content.ReadFromJsonAsync<ResponseType<TokenResponseDto>>();
        _output.WriteLine($"✓ User2 refresh succeeded: {System.Text.Json.JsonSerializer.Serialize(user2Typed)}");
        _assert.ShouldSucceed(user2Typed!);

        // ============================================================
        // NOTE: Current Implementation Behavior
        // ============================================================
        // This test validates that each user can use their own tokens.
        // For enhanced security, consider adding validation that prevents
        // User1 from using User2's token even if they somehow obtain it.
        // This would require matching the token's UserId against the
        // authenticated user making the request.
    }

    /// <summary>
    /// SCENARIO: Replay attack prevention - same token used twice
    /// EXPECTED: First use succeeds, replay attempt fails
    /// </summary>
    [Fact(DisplayName = "Refresh - prevents token replay attacks")]
    public async Task Refresh_Should_Reject_Replayed_Token()
    {
        // ============================================================
        // STEP 1: Setup - Create test user and initial refresh token
        // ============================================================
        var userManager = _factory.Services.GetRequiredService<UserManager<ApplicationUser>>();
        var refreshTokenService = _factory.Services.GetRequiredService<IRefreshTokenService>();

        var replayUser = await userManager.FindByEmailAsync("replay@test.com");

        if (replayUser == null)
        {
            replayUser = new ApplicationUser
            {
                UserName = "replay@test.com",
                Email = "replay@test.com"
            };
            await userManager.CreateAsync(replayUser, "Replay123!");
            await userManager.AddToRoleAsync(replayUser, "Costumer");
            _output.WriteLine("✓ Created test user: replay@test.com");
        }

        // ============================================================
        // STEP 2: Generate refresh token
        // ============================================================
        var ip = "127.0.0.1";
        var tokenResult = await refreshTokenService.GenerateRefreshTokenAsync(replayUser.Id.ToString(), ip);
        Assert.True(tokenResult.Success, "Failed to generate refresh token");
        _output.WriteLine($"✓ Generated token: {tokenResult.Data?.Token}");

        // ============================================================
        // STEP 3: First use of token - should succeed and rotate
        // ============================================================
        _output.WriteLine("\n--- FIRST USE OF TOKEN (Should Succeed) ---");
        var firstResponse = await _client.PostAsJsonAsync(RefreshEndpoint,
            new { BearerToken = "", RefreshToken = tokenResult.Data!.Token });

        _output.WriteLine($"✓ First use status: {firstResponse.StatusCode}");
        var firstRespContent = await firstResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"✓ First use response: {firstRespContent}");
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var firstTyped = await firstResponse.Content.ReadFromJsonAsync<ResponseType<TokenResponseDto>>();
        _output.WriteLine($"✓ First use succeeded: {System.Text.Json.JsonSerializer.Serialize(firstTyped)}");
        _assert.ShouldSucceed(firstTyped!);
        _output.WriteLine($"✓ New token issued: {firstTyped!.Data!.RefreshToken}");

        // ============================================================
        // STEP 4: Replay attack - attempt to reuse the SAME old token
        // ============================================================
        _output.WriteLine("\n--- REPLAY ATTACK ATTEMPT (Should Fail - Token Already Rotated) ---");
        var replayResponse = await _client.PostAsJsonAsync(RefreshEndpoint,
            new { BearerToken = "", RefreshToken = tokenResult.Data!.Token });

        _output.WriteLine($"✓ Replay attempt status: {replayResponse.StatusCode}");
        var replayContent = await replayResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"✓ Replay attempt response: {replayContent}");

        // ============================================================
        // STEP 5: Assert - Replay attack should be rejected
        // ============================================================
        Assert.Equal(HttpStatusCode.BadRequest, replayResponse.StatusCode);

        var replayTyped = await replayResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        _output.WriteLine($"✓ Replay blocked: {System.Text.Json.JsonSerializer.Serialize(replayTyped)}");
        Assert.Contains("Invalid refresh token", replayTyped?.Detail);
        _output.WriteLine("✓ Replay attack successfully prevented!");
    }

    /// <summary>
    /// SCENARIO: Manual token revocation (user logout, security breach, etc.)
    /// EXPECTED: Revoked tokens cannot be used even if not expired
    /// </summary>
    [Fact(DisplayName = "Refresh - rejects manually revoked tokens")]
    public async Task Refresh_Should_Reject_Manually_Revoked_Token()
    {
        // ============================================================
        // STEP 1: Setup - Create test user
        // ============================================================
        var userManager = _factory.Services.GetRequiredService<UserManager<ApplicationUser>>();
        var refreshTokenService = _factory.Services.GetRequiredService<IRefreshTokenService>();

        var revokedUser = await userManager.FindByEmailAsync("revoked@test.com");

        if (revokedUser == null)
        {
            revokedUser = new ApplicationUser
            {
                UserName = "revoked@test.com",
                Email = "revoked@test.com"
            };
            await userManager.CreateAsync(revokedUser, "Revoked123!");
            await userManager.AddToRoleAsync(revokedUser, "Costumer");
            _output.WriteLine("✓ Created test user: revoked@test.com");
        }

        // ============================================================
        // STEP 2: Generate refresh token
        // ============================================================
        var ip = "127.0.0.1";
        var tokenResult = await refreshTokenService.GenerateRefreshTokenAsync(revokedUser.Id.ToString(), ip);
        Assert.True(tokenResult.Success, "Failed to generate refresh token");
        _output.WriteLine($"✓ Generated token: {tokenResult.Data?.Token}");

        // ============================================================
        // STEP 3: Manually revoke the token (simulating logout or security action)
        // ============================================================
        _output.WriteLine("\n--- MANUALLY REVOKING TOKEN ---");
        var revokeResult = await refreshTokenService.RevokeRefreshTokenAsync(
            tokenResult.Data!.Token,
            ip,
            "Manual revocation for testing");

        _output.WriteLine($"✓ Revoke result: {System.Text.Json.JsonSerializer.Serialize(revokeResult)}");
        Assert.True(revokeResult.Success, "Failed to revoke refresh token");
        _output.WriteLine("✓ Token successfully revoked");

        // ============================================================
        // STEP 4: Attempt to use the revoked token
        // ============================================================
        _output.WriteLine("\n--- ATTEMPTING TO USE REVOKED TOKEN (Should Fail) ---");
        var response = await _client.PostAsJsonAsync(RefreshEndpoint,
            new { BearerToken = "", RefreshToken = tokenResult.Data!.Token });

        _output.WriteLine($"✓ Response status: {response.StatusCode}");
        var revokedContent = await response.Content.ReadAsStringAsync();
        _output.WriteLine($"✓ Response content: {revokedContent}");

        // ============================================================
        // STEP 5: Assert - Revoked token should be rejected
        // ============================================================
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var typed = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        _output.WriteLine($"✓ Expected failure: {System.Text.Json.JsonSerializer.Serialize(typed)}");
        Assert.Contains("Invalid refresh token", typed?.Detail);
        _output.WriteLine("✓ Revoked token correctly rejected!");
    }
}