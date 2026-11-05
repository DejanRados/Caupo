using System;

namespace Caupo.Licensing;

public enum LicenseStatus
{
    Active,
    Inactive
}

public enum LicenseOperationalStatus
{
    NotActivated,
    Active,
    GracePeriod,
    Expired,
    Invalid,
    ClockError
}

public sealed record LicenseState(
    LicenseOperationalStatus Status,
    string Message,
    string? LicenseId = null,
    string? DeviceId = null,
    DateTimeOffset? PaidUntil = null,
    DateTimeOffset? GraceUntil = null)
{
    public bool CanOperate => Status is LicenseOperationalStatus.Active or LicenseOperationalStatus.GracePeriod;
    public bool RequiresWarning => Status == LicenseOperationalStatus.GracePeriod;
    public bool RequiresActivation => Status == LicenseOperationalStatus.NotActivated;

    public static LicenseState NotActivated() =>
        new(LicenseOperationalStatus.NotActivated, "Caupo nije aktiviran.");
}