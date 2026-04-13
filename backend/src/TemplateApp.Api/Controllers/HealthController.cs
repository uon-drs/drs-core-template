using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace TemplateApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Unauthenticated health check for load balancer / App Service health probes.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown",
            timestamp = DateTimeOffset.UtcNow,
        });
    }

    /// <summary>
    /// Authenticated health check — verifies JWT Bearer auth is configured correctly.
    /// Returns the claims from the validated token.
    /// </summary>
    [HttpGet("auth")]
    [Authorize]
    public IActionResult GetAuth()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value });
        return Ok(new
        {
            status = "authenticated",
            subject = User.Identity?.Name,
            claims,
        });
    }
}
