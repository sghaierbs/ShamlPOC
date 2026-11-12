using Shaml.Domain.Common;

namespace Shaml.Domain.Geography;

public sealed class District : Entity<long>
{
    public long CityId { get; private set; }

    public string NameAr { get; private set; } = string.Empty;
    public string NameEn { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;

    private District() { }
}