namespace TemplateApp.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.User;
using Auth;
using Services;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
  private readonly UserService _user;

  public UsersController(UserService user) => _user = user;

  /// <summary>
  /// Gets a user by id.
  /// </summary>
  /// <param name="id">User id.</param>
  /// <returns>User model.</returns>
  [HttpGet("{id}")]
  [Authorize(nameof(AuthPolicies.CanViewUsers))]
  [ProducesResponseType(typeof(User), 200)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<ActionResult<User>> Get(string id)
  {
    try
    {
      return Ok(await _user.Get(id));
    }
    catch (KeyNotFoundException ex)
    {
      return NotFound(ex.Message);
    }
  }

  /// <summary>
  /// Lists groups (roles) from Keycloak.
  /// </summary>
  /// <returns>Groups..</returns>
  [HttpGet("groups")]
  [Authorize(nameof(AuthPolicies.CanViewUsers))]
  [ProducesResponseType(typeof(List<Group>), 200)]
  public async Task<ActionResult<List<Group>>> ListGroups() => Ok(await _user.ListGroups());

  /// <summary>
  /// Lists all users.
  /// </summary>
  /// <returns>List of users.</returns>
  [HttpGet]
  [Authorize(nameof(AuthPolicies.CanViewUsers))]
  [ProducesResponseType(typeof(List<User>), 200)]
  public async Task<ActionResult<List<User>>> List() => Ok(await _user.List());

  /// <summary>
  /// Creates a new user.
  /// </summary>
  /// <param name="model">User model.</param>
  /// <returns>Creation result.</returns>
  [HttpPost]
  [Authorize(nameof(AuthPolicies.CanCreateUsers))]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  public async Task<ActionResult> Create(CreateUserRequest model)
  {
    try
    {
      await _user.Create(model);
      return NoContent();
    }
    catch (InvalidOperationException ex)
    {
      return BadRequest(ex.Message);
    }
  }

  /// <summary>
  /// Updates a user.
  /// </summary>
  /// <param name="id">User id.</param>
  /// <param name="model">User model.</param>
  /// <returns>Update result.</returns>
  [HttpPut("{id}")]
  [Authorize(nameof(AuthPolicies.CanUpdateUsers))]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<ActionResult> Set(string id, CreateUserRequest model)
  {
    try
    {
      await _user.Set(id, model);
      return NoContent();
    }
    catch (KeyNotFoundException ex)
    {
      return NotFound(ex.Message);
    }
    catch (InvalidOperationException ex)
    {
      return BadRequest(ex.Message);
    }
  }

  /// <summary>
  /// Deletes a user.
  /// </summary>
  /// <param name="id">User id.</param>
  /// <returns>Deletion result.</returns>
  [HttpDelete("{id}")]
  [Authorize(nameof(AuthPolicies.CanDeleteUsers))]
  [ProducesResponseType(StatusCodes.Status204NoContent)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<ActionResult> Delete(string id)
  {
    try
    {
      await _user.Delete(id);
      return NoContent();
    }
    catch (KeyNotFoundException ex)
    {
      return NotFound(ex.Message);
    }
  }
}
