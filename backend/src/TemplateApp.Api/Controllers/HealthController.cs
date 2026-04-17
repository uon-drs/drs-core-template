using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace TemplateApp.Api.Controllers;

using Auth;
using Models.Emails;
using Services.EmailServices;
using ClaimTypes=System.Security.Claims.ClaimTypes;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
  private readonly HealthEmailService _healthEmail;

  public HealthController(HealthEmailService healthEmail) => _healthEmail = healthEmail;
  /// <summary>
  /// Unauthenticated health check for load balancer / App Service health probes.
  /// </summary>
  [HttpGet]
  [AllowAnonymous]
  public IActionResult Get()
    => Ok(new
    {
      status = "healthy",
      version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown",
      timestamp = DateTimeOffset.UtcNow,
    });

  /// <summary>
  /// Authenticated health check — verifies JWT Bearer auth is configured correctly.
  /// Returns the claims from the validated token.
  /// </summary>
  [HttpGet("auth")]
  [Authorize]
  public IActionResult GetAuth()
  {
    var claims = User.Claims.Select(c => new
    {
      c.Type, c.Value
    });
    return Ok(new
    {
      status = "authenticated", subject = User.Identity?.Name, claims,
    });
  }

  [HttpPost("send-email")]
  [Authorize(nameof(AuthPolicies.CanSendHealthCheckEmail))]
  public async Task<ActionResult> SendEmail()
  {
    var email = User.FindFirst(ClaimTypes.Email)?.Value;

    if (string.IsNullOrWhiteSpace(email))
    {
      return BadRequest("Email is required");
    }

    try
    {
      await _healthEmail.SendHealthCheckEmail(new EmailAddress(email));
      return NoContent();
    }
    catch (Exception ex)
    {
      return StatusCode(500, new
      {
        error = "Failed to send health check email", details = ex.Message
      });
    }
  }
}
