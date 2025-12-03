using System.IdentityModel.Tokens.Jwt;
using Ecommerce.Api;
using Ecommerce.Test.TestUtilities;
using Xunit;
using System.Net.Http.Json;
using System.Net;
using System.Security.Claims;
using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.Wrapper;
using Ecommerce.Test.Authentication.Helpers;
using Microsoft.AspNetCore.Identity.Data;
using Xunit.Abstractions;
using Assert = NUnit.Framework.Assert;

namespace Ecommerce.Test.Authentication;

public class LoginTest : TestBase 
{
    private const string LoginEndpoint = "api/auth/login";
    private readonly HttpClient _client;
    private readonly AssertApiHelper _assert;

    public LoginTest(
        CustomWebApplicationFactory<Program> factory,
        ITestOutputHelper output) : base(factory)
    {
        _client = factory.CreateClient();
        _assert = new AssertApiHelper(output);
    }
    
    //1. Check Invalid Email
    [Fact(DisplayName = "Login - fails for non-existent user")]
    public async Task Login_ShouldFail_WhenEmailDoesNotExist()
    {
        //Arrange
        var payload = new LoginRequest()
        {
            Email = "doesnotexist@test.local",
            Password = "InvalidPassword123@@"
        };
        
        //Act
        var response = await _client.PostAsJsonAsync(LoginEndpoint, payload);

        //Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        var typed = await response.Content.ReadFromJsonAsync<ResponseType<string>>();
        _assert.ShouldFail(typed!,expectedMessage:"Invalid name of password");
    }
    
    [Fact(DisplayName = "Login - fails for wrong password")]
    public async Task Login_Fails_For_WrongPassword()
    {
        //Arrange
        var payload = new LoginRequest()
        {
            Email = "doesnotexist@test.local",
            Password = "InvalidPassword123@@"
        };
        var response = await _client.PostAsJsonAsync(LoginEndpoint, payload);

        Assert.That(HttpStatusCode.BadRequest, Is.EqualTo( response.StatusCode));

        var typed = await response.Content.ReadFromJsonAsync<ResponseType<AuthenticationResponseDto>>();
        _assert.ShouldFail(typed!, expectedMessage: "Invalid name of password");
    }
    [Fact(DisplayName = "Login - succeeds returns access and refresh token and role")]
    public async Task Login_Succeeds_Returns_Tokens_And_Role()
    {
       //Arrange
       var payload = new LoginRequest()
       {
           Email = "doesnotexist@test.local",
           Password = "InvalidPassword123@@"
       };
        // Act
        var response = await _client.PostAsJsonAsync(LoginEndpoint, payload);

        // Assert status
        Assert.That(HttpStatusCode.OK, Is.EqualTo( response.StatusCode));

        // Deserialize wrapper
        var typed = await response.Content.ReadFromJsonAsync<ResponseType<AuthenticationResponseDto>>();
        _assert.ShouldSucceed(typed!);

        var dto = typed!.Data;
        Assert.False(string.IsNullOrWhiteSpace(dto.BearerToken));
        Assert.False(string.IsNullOrWhiteSpace(dto.RefreshToken));
        
        // Validate JWT structure & claims
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(dto.BearerToken);

        // "sub" should be the user id (stringified) - standard practice
        var subClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        Assert.NotNull(subClaim);
        Assert.That(subClaim!.Value, Is.EqualTo(payload.Password.ToString()));

        // role claim exists
        var roleClaim = token.Claims.FirstOrDefault(c => c.Type == "role" || c.Type == ClaimTypes.Role);
        Assert.NotNull(roleClaim);
        Assert.Equals("Customer", roleClaim!.Value);

        // expiry is in the future and matches what API returned
        var exp = dto.ExpiresAt;
        Assert.True(exp > DateTime.UtcNow.AddMinutes(1), "Token expires too soon.");
    }

}

