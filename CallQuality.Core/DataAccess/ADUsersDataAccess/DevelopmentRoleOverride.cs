namespace CallQuality.Core.DataAccess.ADUsersDataAccess;

public sealed class DevelopmentRoleOverride
{
    private static readonly HashSet<string> SupportedRoles =
        new(StringComparer.OrdinalIgnoreCase) { "Manager", "Assessor", "Reassessor", "IT" };

    private readonly HashSet<string> _roles;

    public DevelopmentRoleOverride(Guid userId, IEnumerable<string> roles)
    {
        if (userId == Guid.Empty)
        {
            throw new InvalidOperationException("Set RoleTest:UserId to the test user's Entra object ID.");
        }

        UserId = userId;
        _roles = new HashSet<string>(roles, StringComparer.OrdinalIgnoreCase);

        if (!_roles.IsSubsetOf(SupportedRoles))
        {
            throw new InvalidOperationException("RoleTest:Roles accepts Manager, Assessor, Reassessor and IT.");
        }
    }

    public Guid UserId { get; }

    public bool HasRole(string role) => _roles.Contains(role);
}
