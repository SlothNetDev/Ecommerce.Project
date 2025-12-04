using System.Net;
using System.Net.Http.Json;
using Ecommerce.Api;
using Ecommerce.Infrastructure.Migrations;
using Ecommerce.Shared.AuthenticationDTO;
using Ecommerce.Shared.Wrapper;
using Ecommerce.Test.Authentication.Helpers;
using Ecommerce.Test.TestUtilities;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using Assert = NUnit.Framework.Assert;

namespace Ecommerce.Test.Authentication;

public class RefreshTokenTest(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output)
    : TestBase(factory)
{

    [Fact]
    public async Task Refresh_Should_Return_New_Jwt()
    {
        
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