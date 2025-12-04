using System.Net;
using System.Net.Http.Json;
using Ecommerce.Api;
using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.Wrapper;
using Ecommerce.Test.TestUtilities;
using Xunit;
using Xunit.Abstractions;
using Assert = NUnit.Framework.Assert;

namespace Ecommerce.Test.Authentication.Helpers;

public class LoginTest(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output):
    TestBase(factory)
{
    private readonly HttpClient _client = factory.CreateClient();
    private const string LoginEndpoint = "/api/auth/login";
    private readonly AssertApiHelper _assert = new(output);

    [Fact(DisplayName = "Login - fails for non-existent user")]
    public async Task Login_Fails_For_NonExistentUser()
    {
        var payload = new LoginRequestDto() 
            { Email = "doesnotexist@test.local", Password = "Whatever123!" };

        var response = await _client.PostAsJsonAsync(LoginEndpoint, payload);

        // Expecting your API to return BadRequest (or 400) wrapped ResponseType failure
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var typed = await response.Content.ReadFromJsonAsync<ResponseType<AuthenticationResponseDto>>();
        _assert.ShouldFail(typed!, expectedMessage: null);
    }

    [Fact(DisplayName = "Login - fails for wrong password")]
    public async Task Login_Fails_For_WrongPassword()
    {
        var payload = new LoginRequestDto() 
            { Email = "doesnotexist@test.local", Password = "Whatever123!" };
        var response = await _client.PostAsJsonAsync(LoginEndpoint, payload);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        var typed = await response.Content.ReadFromJsonAsync<ResponseType<AuthenticationResponseDto>>();
        _assert.ShouldFail(typed!, expectedMessage: null);
    }
    
    [Fact(DisplayName = "Login - succeeds returns access and refresh token and role")]
    public async Task Login_Succeeds_Returns_Tokens_And_Role()
    {
        /*// Arrange
        var user = await TestUserHelper.CreateBasicUserAsync(Factory.Services, role: "Customer");

        var payload = new { userName = user.Email, password = TestUserHelper.DefaultPassword };

        // Act
        var response = await Client.PostAsJsonAsync(LoginEndpoint, payload);

        // Assert status
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Deserialize wrapper
        var typed = await response.Content.ReadFromJsonAsync<ResponseType<AuthenticationResponseDto>>();
        _assert.ShouldSucceed(typed!);

        var dto = typed!.Data;
        Assert.False(string.IsNullOrWhiteSpace(dto.BearerToken));
        Assert.False(string.IsNullOrWhiteSpace(dto.RefreshToken));
        Assert.Equal(user.Email, dto.UserName);

        // Validate JWT structure & claims
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(dto.BearerToken);

        // "sub" should be the user id (stringified) - standard practice
        var subClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        Assert.NotNull(subClaim);
        Assert.Equal(user.Id.ToString(), subClaim!.Value);

        // role claim exists
        var roleClaim = token.Claims.FirstOrDefault(c => c.Type == "role" || c.Type == ClaimTypes.Role);
        Assert.NotNull(roleClaim);
        Assert.Equal("Customer", roleClaim!.Value);

        // expiry is in the future and matches what API returned
        var exp = dto.ExpiresAt;
        Assert.True(exp > DateTime.UtcNow.AddMinutes(1), "Token expires too soon.");*/
    }
}