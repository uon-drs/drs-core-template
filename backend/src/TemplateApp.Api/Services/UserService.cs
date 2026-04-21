namespace TemplateApp.Api.Services;

using Config;
using Keycloak.AuthServices.Sdk.Kiota.Admin;
using Keycloak.AuthServices.Sdk.Kiota.Admin.Models;
using Microsoft.Extensions.Options;
using Models.User;

public class UserService
{
  private readonly KeycloakAdminApiClient _keycloakClient;
  private readonly KeycloakOptions _keycloakOptions;

  public UserService(
    KeycloakAdminApiClient keycloakUserClient,
    IOptions<KeycloakOptions> keycloakOptions
  )
  {
    _keycloakClient = keycloakUserClient;
    _keycloakOptions = keycloakOptions.Value;
  }

  /// <summary>
  /// Get user from Keycloak by id.
  /// </summary>
  /// <param name="id">User ID.</param>
  /// <returns>User</returns>
  public async Task<User> Get(string id)
  {
    var realm = _keycloakClient.Admin.Realms[_keycloakOptions.Realm];

    var user = await realm.Users[id].GetAsync() ?? throw new KeyNotFoundException($"User with id {id} not found");
    var groups = await realm.Users[id].Groups.GetAsync() ?? [];

    return new User(user, groups);
  }

  /// <summary>
  /// List all users from Keycloak.
  /// </summary>
  /// <returns>User list</returns>
  public async Task<List<User>> List()
  {
    var realm = _keycloakClient.Admin.Realms[_keycloakOptions.Realm];

    var groups = await realm.Groups.GetAsync() ?? [];
    var users = await realm.Users.GetAsync() ?? [];

    var userGroupsMap = new Dictionary<string, List<GroupRepresentation>>();

    foreach (var group in groups)
    {
      if (group.Id is null)
      {
        continue;
      }

      var members = await realm.Groups[group.Id].Members.GetAsync() ?? [];
      foreach (var member in members)
      {
        if (member.Id is null)
        {
          continue;
        }

        if (!userGroupsMap.TryGetValue(member.Id, out var memberGroups))
        {
          memberGroups = [];
          userGroupsMap[member.Id] = memberGroups;
        }
        memberGroups.Add(group);
      }
    }

    return users
      .Where(x => x.Id is not null)
      .Select(x => new User(x, userGroupsMap.GetValueOrDefault(x.Id!, [])))
      .ToList();
  }

  /// <summary>
  /// Create a new user in Keycloak.
  /// </summary>
  /// <param name="model">Model containing user data.</param>
  public async Task Create(CreateUserRequest model)
  {
    var realm = _keycloakClient.Admin.Realms[_keycloakOptions.Realm];

    var existingUsers = await realm.Users.GetAsync(config =>
    {
      config.QueryParameters.Email = model.Email;
    }) ?? [];

    if (existingUsers.Count != 0)
    {
      throw new InvalidOperationException($"User with email {model.Email} already exists");
    }

    var groups = (await realm.Groups.GetAsync() ?? [])
      .Select(x => x.Name)
      .Where(x => !string.IsNullOrEmpty(x))
      .ToList();

    var invalidGroups = model.Groups.Where(x => !groups.Contains(x)).ToList();
    if (invalidGroups.Count != 0)
    {
      throw new InvalidOperationException($"Invalid role: {string.Join(", ", invalidGroups)}");
    }

    await realm.Users.PostAsync(new UserRepresentation
    {
      Email = model.Email, Username = model.Email, Groups = model.Groups, Enabled = model.IsEnabled
    });
  }

  /// <summary>
  /// Update user.
  /// </summary>
  /// <param name="id"> User ID.</param>
  /// <param name="model">Model containing user data.</param>
  public async Task Set(string id, CreateUserRequest model)
  {
    var realm = _keycloakClient.Admin.Realms[_keycloakOptions.Realm];

    var existing = await realm.Users[id].GetAsync() ?? throw new KeyNotFoundException("User not found");

    var groups = (await realm.Groups.GetAsync() ?? [])
      .Select(x => new
      {
        x.Name, x.Id
      })
      .Where(x => !string.IsNullOrEmpty(x.Name))
      .ToList();

    var invalidGroups = model.Groups.Where(x => groups.All(y => y.Name != x)).ToList();
    if (invalidGroups.Count != 0)
    {
      throw new InvalidOperationException($"Invalid role: {string.Join(", ", invalidGroups)}");
    }

    await realm.Users[id].PutAsync(new UserRepresentation
    {
      Enabled = model.IsEnabled
    });

    var userGroups = (await realm.Users[id].Groups.GetAsync() ?? []).ToList();
    var groupsToAdd = model.Groups
      .Where(x => userGroups.All(y => y.Name != x))
      .Select(x => groups.First(y => y.Name == x).Id)
      .ToList();

    var groupsToRemove = userGroups
      .Where(x => !model.Groups.Contains(x.Name!))
      .Select(x => x.Id)
      .ToList();

    foreach (var groupId in groupsToRemove)
    {
      await realm.Users[id].Groups[groupId!].DeleteAsync();
    }

    foreach (var groupId in groupsToAdd)
    {
      await realm.Users[id].Groups[groupId!].PutAsync();
    }
  }

  /// <summary>
  /// Delete a user from Keycloak.
  /// </summary>
  /// <param name="id">User ID.</param>
  public async Task Delete(string id)
  {
    var realm = _keycloakClient.Admin.Realms[_keycloakOptions.Realm];

    var user = await realm.Users[id].GetAsync() ??
               throw new KeyNotFoundException("User not found");

    await realm.Users[id].DeleteAsync();
  }

  /// <summary>
  /// List users in a group.
  /// </summary>
  /// <param name="id">Group ID.</param>
  /// <returns>Users.</returns>
  public async Task<List<PartialUser>> ListGroupUsers(string id)
  {
    var realm = _keycloakClient.Admin.Realms[_keycloakOptions.Realm];
    var members = await realm.Groups[id].Members.GetAsync() ?? [];
    return members.Select(x => new PartialUser(x.FirstName, x.LastName, x.Email, x.Enabled ?? false)).ToList();
  }

  /// <summary>
  /// List groups.
  /// </summary>
  /// <returns>Groups.</returns>
  public async Task<List<Group>> ListGroups()
  {
    var realm = _keycloakClient.Admin.Realms[_keycloakOptions.Realm];
    var groups = await realm.Groups.GetAsync() ?? [];
    return groups.Where(x => x.Id is not null && x.Name is not null)
      .Select(x => new Group(x.Id!, x.Name!))
      .ToList();
  }
}
