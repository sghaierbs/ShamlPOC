using Shaml.Domain.Common;

namespace Shaml.Domain.Geography;

public sealed class City : Entity<long>
{
    public long RegionId { get; private set; }

    public string NameAr { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;

    private City() { }
}