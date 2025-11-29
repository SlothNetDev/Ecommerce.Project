using System.Net.Http.Headers;
using Ecommerce.Api;
using Ecommerce.Test.TestUtilities;
using Xunit;

namespace Ecommerce.Test;

/// <summary>
/// Shared Factory: Reuses the same application factory across tests
/// HTTP Client: Provides both unauthorized and authorized clients                                        
/// CreateAuthorizedClient(): Easy method to get pre-authenticated clients
/// </summary>
public abstract class TestBase : IClassFixture<CustomWebApplicationFactory<Program>>
{
    protected readonly CustomWebApplicationFactory<Program> _factory;
    protected readonly HttpClient _client;

    protected TestBase(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    protected HttpClient CreateAuthorizedClient(string email)
    {
        var token = TestUserHelper.GenerateJwtForUser(_factory.Services, email);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return client;
    }
}