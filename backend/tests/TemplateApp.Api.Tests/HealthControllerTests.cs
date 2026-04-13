using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

namespace TemplateApp.Api.Tests;

/// <summary>
/// Integration tests for HealthController using the real ASP.NET Core pipeline.
/// Add a test database or mock EF Core if you need database access in tests.
/// </summary>
public class HealthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOk_WithoutAuthentication()
    {
        var response = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetHealthAuth_ReturnsUnauthorized_WithoutToken()
    {
        var response = await _client.GetAsync("/api/health/auth");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
