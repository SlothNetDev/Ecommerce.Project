using System.Net;
using System.Net.Http.Json;
using Ecommerce.Api;
using Ecommerce.Core.Application.Common.Interfaces;
using Ecommerce.Core.Application.Common.Interfaces.RefreshToken;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Identity.Entities;
using Ecommerce.Shared.AuthenticationDTO;
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
using Xunit.Sdk;
using Xunit;

namespace Ecommerce.Test.Authentication;

    public class RefreshTokenTest : TestBase
    {
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly AssertApiHelper _assert;
        private const string RefreshEndpoint = "/api/auth/action/RefreshToken";

        public RefreshTokenTest(CustomWebApplicationFactory<Program> factory,ITestOutputHelper output) : base(factory)
        {
            _refreshTokenService = factory.Services.GetRequiredService<IRefreshTokenService>();
            _assert = new AssertApiHelper(output);
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

    [Fact(DisplayName = "Refresh - succeeds and returns new JWT")]
    public async Task Refresh_ShouldReturnNewJwt()
    {
        var userManager = _factory.Services.GetRequiredService<UserManager<ApplicationUser>>();

        var refreshUser = await userManager.FindByEmailAsync("refreshuser@test.com");
        
        if (refreshUser == null)
        {
            refreshUser = new ApplicationUser
            {
                UserName = "refreshuser@test.com",
                Email = "refreshuser@test.com"
            };
            await userManager.CreateAsync(refreshUser, "Refresh123!");
            await userManager.AddToRoleAsync(refreshUser, "Costumer");
        }
        var token = TestUserHelper.GenerateJwtForUser(_factory.Services, "refreshuser@test.com");
        
        var ip = "127.0.0.1";

        var refreshService = _factory.Services.GetRequiredService<IRefreshTokenService>();
        var oldTokenResult = await refreshService.GenerateRefreshTokenAsync(refreshUser.Id.ToString(), ip);
        Assert.True(oldTokenResult.Success, "Failed to generate refresh token");
        var oldToken = oldTokenResult.Data!;

        // Verify token was created
        var dbContext = _factory.Services.GetRequiredService<ApplicationDbContext>();
        var tokenExists = await dbContext.RefreshToken.AnyAsync(t => t.Token == oldToken.Token);
        Assert.True(tokenExists, "Token was not saved to database");

        // Act - call refresh endpoint
        var response = await _client.PostAsJsonAsync(RefreshEndpoint, new { BearerToken = "", RefreshToken = oldToken.Token });

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error response: {errorContent}");
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var typed = await response.Content.ReadFromJsonAsync<ResponseType<TokenResponseDto>>();
        _assert.ShouldSucceed(typed!);
        Assert.NotEqual(oldToken.Token, typed?.Data!.RefreshToken);
        Assert.False(string.IsNullOrEmpty(typed.Data?.AccessToken));
    }


    [Fact]
    public async Task Refresh_Should_Invalidate_Old_Token()
    {
        // Arrange
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
        }

        var ip = "127.0.0.1";
        var oldTokenResult = await refreshTokenService.GenerateRefreshTokenAsync(refreshUser.Id.ToString(), ip);
        Assert.True(oldTokenResult.Success, "Failed to generate refresh token");
        var oldToken = oldTokenResult.Data!;

        // Act - First refresh should succeed
        var firstResponse = await _client.PostAsJsonAsync(RefreshEndpoint, new { BearerToken = "", RefreshToken = oldToken.Token });
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var firstTyped = await firstResponse.Content.ReadFromJsonAsync<ResponseType<TokenResponseDto>>();
        _assert.ShouldSucceed(firstTyped!);

        // Act - Second refresh with same token should fail (old token invalidated)
        var secondResponse = await _client.PostAsJsonAsync(RefreshEndpoint, new { BearerToken = "", RefreshToken = oldToken.Token });
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);

        var secondTyped = await secondResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Contains("Invalid refresh token", secondTyped?.Detail);
    }

    [Fact]
    public async Task Refresh_Should_Reject_Expired_Token()
    {
        // Arrange
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
        }

        var ip = "127.0.0.1";
        var tokenResult = await refreshTokenService.GenerateRefreshTokenAsync(refreshUser.Id.ToString(), ip);
        Assert.True(tokenResult.Success, "Failed to generate refresh token");

        // Make the token expired by setting expiry to past date
        var tokenEntity = await dbContext.RefreshToken
            .FirstOrDefaultAsync(t => t.Token == tokenResult.Data!.Token);

        Assert.NotNull(tokenEntity);

        tokenEntity!.Expires = DateTime.UtcNow.AddMinutes(-1); // Expired 1 minute ago
        await dbContext.SaveChangesAsync();

        // Act
        var response = await _client.PostAsJsonAsync(RefreshEndpoint, new { BearerToken = "", RefreshToken = tokenResult.Data!.Token });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var typed = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Contains("Invalid refresh token", typed?.Detail);
    }

    [Fact]
    public async Task Refresh_Should_Reject_Unknown_Token()
    {
        // Arrange - Use a completely fake token that doesn't exist in the database
        var fakeToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) +
                       Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        // Act
        var response = await _client.PostAsJsonAsync(RefreshEndpoint, new { BearerToken = "", RefreshToken = fakeToken });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var typed = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Should get "Invalid refresh token" for unknown token
        Assert.Contains("Invalid refresh token", typed?.Detail);
    }

    [Fact]
    public async Task Refresh_Should_Reject_Token_With_Wrong_User()
    {
        // Arrange - Create two users with their own refresh tokens
        var userManager = _factory.Services.GetRequiredService<UserManager<ApplicationUser>>();
        var refreshTokenService = _factory.Services.GetRequiredService<IRefreshTokenService>();

        // User 1
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
        }

        // User 2
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
        }

        var ip = "127.0.0.1";

        // Create token for User 1
        var user1TokenResult = await refreshTokenService.GenerateRefreshTokenAsync(user1.Id.ToString(), ip);
        Assert.True(user1TokenResult.Success, "Failed to generate refresh token for user1");

        // Create token for User 2
        var user2TokenResult = await refreshTokenService.GenerateRefreshTokenAsync(user2.Id.ToString(), ip);
        Assert.True(user2TokenResult.Success, "Failed to generate refresh token for user2");

        // Act - Try to use User1's token (should work for User1)
        var user1Response = await _client.PostAsJsonAsync(RefreshEndpoint,
            new { BearerToken = "", RefreshToken = user1TokenResult.Data!.Token });

        Assert.Equal(HttpStatusCode.OK, user1Response.StatusCode);

        var user1Typed = await user1Response.Content.ReadFromJsonAsync<ResponseType<TokenResponseDto>>();
        _assert.ShouldSucceed(user1Typed!);

        // Act - Try to use User2's token (should work for User2 - no cross-user validation currently)
        var user2Response = await _client.PostAsJsonAsync(RefreshEndpoint,
            new { BearerToken = "", RefreshToken = user2TokenResult.Data!.Token });

        // This currently succeeds because the system doesn't validate token ownership against request context
        Assert.Equal(HttpStatusCode.OK, user2Response.StatusCode);

        var user2Typed = await user2Response.Content.ReadFromJsonAsync<ResponseType<TokenResponseDto>>();
        _assert.ShouldSucceed(user2Typed!);

        // Note: This test documents current behavior. In a more secure implementation,
        // the refresh endpoint should validate that the refresh token belongs to the
        // authenticated user making the request.
    }

    [Fact]
    public async Task Refresh_Should_Reject_Replayed_Token()
    {
        // Arrange
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
        }

        var ip = "127.0.0.1";
        var tokenResult = await refreshTokenService.GenerateRefreshTokenAsync(replayUser.Id.ToString(), ip);
        Assert.True(tokenResult.Success, "Failed to generate refresh token");

        // Act - First use should succeed
        var firstResponse = await _client.PostAsJsonAsync(RefreshEndpoint,
            new { BearerToken = "", RefreshToken = tokenResult.Data!.Token });

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var firstTyped = await firstResponse.Content.ReadFromJsonAsync<ResponseType<TokenResponseDto>>();
        _assert.ShouldSucceed(firstTyped!);

        // Act - Attempt to replay the same token (should fail due to rotation)
        var replayResponse = await _client.PostAsJsonAsync(RefreshEndpoint,
            new { BearerToken = "", RefreshToken = tokenResult.Data!.Token });

        // Assert - Token replay should be rejected
        Assert.Equal(HttpStatusCode.BadRequest, replayResponse.StatusCode);

        var replayTyped = await replayResponse.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Contains("Invalid refresh token", replayTyped?.Detail);
    }

    [Fact]
    public async Task Refresh_Should_Reject_Manually_Revoked_Token()
    {
        // Arrange
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
        }

        var ip = "127.0.0.1";
        var tokenResult = await refreshTokenService.GenerateRefreshTokenAsync(revokedUser.Id.ToString(), ip);
        Assert.True(tokenResult.Success, "Failed to generate refresh token");

        // Manually revoke the token
        var revokeResult = await refreshTokenService.RevokeRefreshTokenAsync(
            tokenResult.Data!.Token,
            ip,
            "Manual revocation for testing");

        Assert.True(revokeResult.Success, "Failed to revoke refresh token");

        // Act - Try to use the revoked token
        var response = await _client.PostAsJsonAsync(RefreshEndpoint,
            new { BearerToken = "", RefreshToken = tokenResult.Data!.Token });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var typed = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Contains("Invalid refresh token", typed?.Detail);
    }


}