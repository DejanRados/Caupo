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
    DateTimeOffset? GraceUntil = null,
    string? PlanCode = null,
    string? FingerprintSha256 = null)
{
    public bool CanOperate => Status is LicenseOperationalStatus.Active or LicenseOperationalStatus.GracePeriod;
    public bool RequiresWarning => Status == LicenseOperationalStatus.GracePeriod;
    public bool RequiresActivation => Status == LicenseOperationalStatus.NotActivated;
    public bool IsTrial => PlanCode == "trial";

    public static LicenseState NotActivated() =>
        new(LicenseOperationalStatus.NotActivated, "Caupo nije aktiviran.");
}