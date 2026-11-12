namespace Shaml.Domain.Identity;

public sealed class RolePermission
{
    private RolePermission() { } // EF Core

    public RolePermission(long roleId, long permissionId)
    {
        if (roleId <= 0)
            throw new ArgumentOutOfRangeException(nameof(roleId));

        if (permissionId <= 0)
            throw new ArgumentOutOfRangeException(nameof(permissionId));

        RoleId = roleId;
        PermissionId = permissionId;
    }

    public long RoleId { get; private set; }
    public long PermissionId { get; private set; }
}