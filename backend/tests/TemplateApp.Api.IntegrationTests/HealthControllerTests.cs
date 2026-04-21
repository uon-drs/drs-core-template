namespace TemplateApp.Api.Tests;

using System.Net;
using Constants;
using Fixtures;
using Xunit;

public class HealthControllerTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
  [Fact]
  public async Task GetHealth_ReturnsOk_WithoutAuthentication()
  {
    var response = await factory.CreateAnonymousClient().GetAsync("/api/health");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task GetHealthAuth_ReturnsUnauthorized_WithoutToken()
  {
    var response = await factory.CreateAnonymousClient().GetAsync("/api/health/auth");

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task SendEmail_ReturnsUnauthorized_WithoutToken()
  {
    var response = await factory.CreateAnonymousClient().PostAsync("/api/health/send-email", null);

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }

  [Fact]
  public async Task GetHealthAuth_ReturnsOk_WithValidToken()
  {
    var client = factory.CreateAuthenticatedClient([TestClaims.Name]);

    var response = await client.GetAsync("/api/health/auth");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
  }

  [Fact]
  public async Task SendEmail_ReturnsForbidden_WhenMissingRequiredRole()
  {
    var client = factory.CreateAuthenticatedClient([TestClaims.Name]);

    var response = await client.PostAsync("/api/health/send-email", null);

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  [Fact]
  public async Task SendEmail_ReturnsBadRequest_WhenEmailClaimMissing()
  {
    var client = factory.CreateAuthenticatedClient([TestClaims.Name, TestClaims.ViewUsersRole]);

    var response = await client.PostAsync("/api/health/send-email", null);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

  [Fact]
  public async Task SendEmail_ReturnsNoContent_WhenAuthorizedWithEmailClaim()
  {
    var client = factory.CreateAuthenticatedClient([TestClaims.Name, TestClaims.Email, TestClaims.ViewUsersRole]);

    var response = await client.PostAsync("/api/health/send-email", null);

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
  }
}
