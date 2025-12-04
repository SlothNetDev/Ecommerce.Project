using System.Net;
using System.Net.Http.Json;
using Ecommerce.Api;
using Ecommerce.Core.Application.Common.Interfaces;
using Ecommerce.Infrastructure.Migrations;
using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.Wrapper;
using Ecommerce.Test.Authentication.Helpers;
using Ecommerce.Test.TestUtilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using Assert = NUnit.Framework.Assert;

namespace Ecommerce.Test.Authentication;

public class RefreshTokenTest : TestBase
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly AssertApiHelper _assert;
    private const string RefreshEndpoint = "/api/auth/refresh";
    
    public RefreshTokenTest(CustomWebApplicationFactory<Program> factory,ITestOutputHelper output) : base(factory)
    {
        _refreshTokenService = factory.Services.GetRequiredService<IRefreshTokenService>();
        _assert = new AssertApiHelper(output);
    }

    [Fact(DisplayName = "Refresh - succeeds and returns new JWT")]
    public async Task Refresh_ShouldReturnNewJwt()
    {
        var user = TestUserHelper.GenerateJwtForUser(_factory.Services, "refreshuser@test.com");
        var ip = "127.0.0.1";

        var refreshService = _factory.Services.GetRequiredService<IRefreshTokenService>();
        var oldToken = refreshService.GenerateRefreshToken(user.ToString(), ip);
        await refreshService.SaveRefreshTokenAsync(oldToken);

        // Act - call refresh endpoint
        var response = await _client.PostAsJsonAsync(RefreshEndpoint, new { RefreshToken = oldToken.Token });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var typed = await response.Content.ReadFromJsonAsync<ResponseType<AuthenticationResponseDto>>();
        _assert.ShouldSucceed(typed!);
        Assert.That(typed?.Data!.RefreshToken, Is.Not.EqualTo(oldToken.Token));
        Assert.IsNotEmpty(typed.Data?.BearerToken);
    }


    [Fact]
    public async Task Refresh_Should_Invalidate_Old_Token()
    {
        
    }

    [Fact]
    public async Task Refresh_Should_Reject_Expired_Token()
    {
        
    }

    [Fact]
    public async Task Refresh_Should_Reject_Unknown_Token()
    {
        
    }

    [Fact]
    public async Task Refresh_Should_Reject_Token_With_Wrong_User()
    {
        
    }

    [Fact]
    public async Task Refresh_Should_Reject_Replayed_Token()
    {
        
    }

    [Fact]
    public async Task Refresh_Should_Reject_Manually_Revoked_Token()
    {
        
    }


}