using System.Net;
using System.Net.Http.Json;
using DocumentSubmission.Api.Contracts;
using DocumentSubmission.Api.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using static DocumentSubmission.Api.IntegrationTests.Infrastructure.Users;

namespace DocumentSubmission.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AuthTests(ApiFactory factory)
{
    [Fact]
    public async Task Login_KnownUser_ReturnsTokenWithClaims()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/auth/login", new { userId = ClaraReviewerNl });

        var login = await ApiClient.ReadAsync<LoginResponse>(response);
        Assert.True(login.ExpiresAt > DateTime.UtcNow);
        var token = new JsonWebToken(login.Token);
        Assert.Equal(ClaraReviewerNl.ToString(), token.GetClaim("sub").Value);
        Assert.Equal("Reviewer", token.GetClaim("role").Value);
        Assert.Equal("NL", token.GetClaim("country").Value);
    }

    [Fact]
    public async Task Login_UnknownUser_Returns401()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/auth/login", new { userId = Guid.NewGuid() });

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_OutsideDevelopment_Returns404()
    {
        using var production = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

        var response = await production.CreateClient().PostAsJsonAsync("/auth/login", new { userId = AliceEditorNl });

        await ProblemAssert.IsProblemAsync(response, HttpStatusCode.NotFound);
    }
}
