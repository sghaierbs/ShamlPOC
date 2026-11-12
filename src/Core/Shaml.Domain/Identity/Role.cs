using Shaml.Domain.Common;

namespace Shaml.Domain.Identity;

public sealed class Role : Entity<long>
{
    private Role() { } // EF Core

    public Role(string code, string name, RoleScope scope)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Role code is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Role name is required.");

        if (!Enum.IsDefined(scope))
            throw new ArgumentOutOfRangeException(nameof(scope));

        Code = code.Trim();
        Name = name.Trim();
        Scope = scope;
        IsActive = true;
    }

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;

    public RoleScope Scope { get; private set; }

    public bool IsActive { get; private set; }

    public void Deactivate()
    {
        IsActive = false;
    }
}