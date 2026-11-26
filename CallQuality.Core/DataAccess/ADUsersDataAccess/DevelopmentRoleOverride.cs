namespace CallQuality.Core.DataAccess.ADUsersDataAccess;

public sealed class DevelopmentRoleOverride
{
    private static readonly HashSet<string> SupportedRoles =
        new(StringComparer.OrdinalIgnoreCase) { "Manager", "Assessor", "Reassessor", "IT" };

    private readonly HashSet<string> _roles;

    public DevelopmentRoleOverride(IEnumerable<string> roles)
    {
        _roles = new HashSet<string>(roles, StringComparer.OrdinalIgnoreCase);

        if (!_roles.IsSubsetOf(SupportedRoles))
        {
            throw new InvalidOperationException("RoleTest:Roles accepts Manager, Assessor, Reassessor and IT.");
        }
    }

    public bool HasRole(string role) => _roles.Contains(role);
}
