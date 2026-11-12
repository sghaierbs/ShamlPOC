using Shaml.Domain.Common;

namespace Shaml.Domain.Identity;

public sealed class Permission : Entity<long>
{
    private Permission() { } // EF Core

    public Permission(string code, string description)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Permission code is required.");

        Code = code.Trim();
        Description = description?.Trim() ?? string.Empty;
    }

    public string Code { get; private set; } = null!;
    public string Description { get; private set; } = string.Empty;
}