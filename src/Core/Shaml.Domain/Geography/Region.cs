using Shaml.Domain.Common;

namespace Shaml.Domain.Geography;

public sealed class Region : Entity<long>
{
    public string NameAr { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private Region() { }
}