
using Shaml.Domain.Common;

namespace Shaml.Domain.Centers;

public sealed class Center : Entity<long>
{
    public string Name { get; private set; } = string.Empty;
    public string DisplayName => $"{Name} - {Id}";
    public long RegionId { get; private set; }
    public long CityId { get; private set; }
    public long? DistrictId { get; private set; }

    public int OperationalCapacity { get; private set; }
    public int WaitingListLimit { get; private set; }

    public bool IsActive { get; private set; }
    public bool IsReceivingCases { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private Center()
    {
    }

    public Center(
        string name,
        long regionId,
        long cityId,
        long? districtId,
        int operationalCapacity,
        int waitingListLimit)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Center name is required.", nameof(name));

        if (name.Trim().Length > 200)
            throw new ArgumentException(
                "Center name cannot exceed 200 characters.",
                nameof(name));

        if (regionId <= 0)
            throw new ArgumentOutOfRangeException(nameof(regionId));

        if (cityId <= 0)
            throw new ArgumentOutOfRangeException(nameof(cityId));

        if (districtId.HasValue && districtId.Value <= 0)
            throw new ArgumentOutOfRangeException(nameof(districtId));

        if (operationalCapacity <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(operationalCapacity));

        if (waitingListLimit < 0)
            throw new ArgumentOutOfRangeException(
                nameof(waitingListLimit));

        Name = name.Trim();
        RegionId = regionId;
        CityId = cityId;
        DistrictId = districtId;

        OperationalCapacity = operationalCapacity;
        WaitingListLimit = waitingListLimit;

        IsActive = true;
        IsReceivingCases = true;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name) ||
            name.Trim().Length > 200)
        {
            throw new ArgumentException(
                "Center name must contain 1 to 200 characters.",
                nameof(name));
        }

        Name = name.Trim();
    }

    public void UpdateCapacity(
        int operationalCapacity,
        int waitingListLimit)
    {
        if (operationalCapacity <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(operationalCapacity));

        if (waitingListLimit < 0)
            throw new ArgumentOutOfRangeException(
                nameof(waitingListLimit));

        OperationalCapacity = operationalCapacity;
        WaitingListLimit = waitingListLimit;
    }

    public void UpdateLocation(
        long regionId,
        long cityId,
        long? districtId)
    {
        if (regionId <= 0 || cityId <= 0)
            throw new ArgumentException(
                "Region and city are required.");

        if (districtId.HasValue && districtId.Value <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(districtId));

        RegionId = regionId;
        CityId = cityId;
        DistrictId = districtId;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
        IsReceivingCases = false;
    }

    public void EnableReception()
    {
        if (!IsActive)
            throw new InvalidOperationException(
                "An inactive center cannot receive cases.");

        IsReceivingCases = true;
    }

    public void DisableReception()
    {
        IsReceivingCases = false;
    }
}
