namespace TemplateApp.Api.Data.Seeder;

using Auth;
using Config;
using Keycloak.AuthServices.Sdk.Kiota.Admin;
using Keycloak.AuthServices.Sdk.Kiota.Admin.Models;
using Microsoft.Extensions.Options;

/// <summary>
/// Keycloak data seeder to seed groups, roles and map roles to groups.
/// </summary>
public class KeycloakDataSeeder
{
  private readonly KeycloakAdminApiClient _keycloakClient;
  private readonly KeycloakOptions _keycloakOptions;

  public KeycloakDataSeeder(
    KeycloakAdminApiClient keycloakClient,
    IOptions<KeycloakOptions> keycloakOptions
  )
  {
    _keycloakClient = keycloakClient;
    _keycloakOptions = keycloakOptions.Value;
  }

  /// <summary>
  /// Seed Keycloak data including groups, roles and mapping roles to groups.
  /// </summary>
  public async Task SeedKeycloakData()
  {
    await SeedGroups();
    await SeedRoles();
    await MapRolesToGroups();
  }

  /// <summary>
  /// Seed groups.
  /// </summary>
  private async Task SeedGroups()
  {
    var realm = _keycloakClient.Admin.Realms[_keycloakOptions.Realm];
    var existingGroups = await realm.Groups.GetAsync() ?? [];

    var groups = new List<GroupRepresentation>
    {
      new GroupRepresentation
      {
        Name = Groups.Admin
      },
      new GroupRepresentation()
      {
        Name = Groups.Guest
      }
    };

    foreach (var group in groups)
    {
      var existingGroup = existingGroups.FirstOrDefault(x => x.Name == group.Name);
      if (existingGroup is null)
      {
        await realm.Groups.PostAsync(group);
      }
    }
  }

  /// <summary>
  /// Seed roles.
  /// </summary>
  private async Task SeedRoles()
  {
    var realm = _keycloakClient.Admin.Realms[_keycloakOptions.Realm];
    var existingRoles = await realm.Roles.GetAsync() ?? [];

    var roles = new List<RoleRepresentation>
    {
      new RoleRepresentation
      {
        Name = Roles.CreateUsers
      },
      new RoleRepresentation
      {
        Name = Roles.UpdateUsers
      },
      new RoleRepresentation
      {
        Name = Roles.DeleteUsers
      },
      new RoleRepresentation
      {
        Name = Roles.ViewUsers
      },
      new RoleRepresentation
      {
        Name = Roles.ViewContent
      },
    };

    foreach (var role in roles)
    {
      var existingRole = existingRoles.FirstOrDefault(x => x.Name == role.Name);
      if (existingRole is not null)
      {
        continue;
      }

      await realm.Roles.PostAsync(role);
    }
  }

  /// <summary>
  /// Map roles to groups.
  /// </summary>
  private async Task MapRolesToGroups()
  {
    var realm = _keycloakClient.Admin.Realms[_keycloakOptions.Realm];
    var groups = await realm.Groups.GetAsync() ?? [];

    var adminGroup = groups.FirstOrDefault(x => x.Name == Groups.Admin);
    var guestGroup = groups.FirstOrDefault(x => x.Name == Groups.Guest);

    if (adminGroup is null || guestGroup is null)
    {
      throw new InvalidOperationException("Admin or Guest group not found");
    }

    var adminRoles = new List<string>
    {
      Roles.CreateUsers, Roles.UpdateUsers, Roles.DeleteUsers, Roles.ViewUsers,  Roles.ViewContent
    };

    var guestRoles = new List<string>
    {
      Roles.ViewContent
    };


    var existingRoles = await realm.Roles.GetAsync() ?? [];

    await AssignRolesToGroup(adminGroup.Id!, existingRoles.Where(x => adminRoles.Contains(x.Name!)).ToList());
    await AssignRolesToGroup(guestGroup.Id!, existingRoles.Where(x => guestRoles.Contains(x.Name!)).ToList());
  }

  /// <summary>
  /// Map roles to group
  /// </summary>
  /// <param name="id">Group ID.</param>
  /// <param name="roles">Roles to be mapped.</param>
  private async Task AssignRolesToGroup(string id, List<RoleRepresentation> roles)
  {
    var realm = _keycloakClient.Admin.Realms[_keycloakOptions.Realm];
    await realm.Groups[id].RoleMappings.Realm.PostAsync(roles);
  }
}
