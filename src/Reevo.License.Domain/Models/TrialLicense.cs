﻿using System.Text.Json.Serialization;
using Reevo.License.Domain.Shared.Enum;

namespace Reevo.License.Domain.Models;

[JsonDerivedType(typeof(TrialLicense), nameof(LicenseType.Trial))]
public class TrialLicense : BaseLicense
{
    [JsonConstructor]
    protected TrialLicense()
    {
        Type = LicenseType.Trial;
    }

    public TrialLicense(TimeSpan trialPeriod)
    {
        TrialPeriod = trialPeriod;
        ExpirationDate = DateTime.UtcNow.Add(trialPeriod);
        Type = LicenseType.Trial;
    }

    public TrialLicense(BaseLicense license, TimeSpan trialPeriod)
    {
        TrialPeriod = trialPeriod;
        ExpirationDate = DateTime.UtcNow.Add(trialPeriod);
        Type = LicenseType.Trial;
        Features = license.Features;
        Issuer = license.Issuer;
        LicenseId = license.LicenseId;
        LicenseKey = license.LicenseKey;
        Type = license.Type;
        IssuedOn = license.IssuedOn;
    }

    [JsonInclude] public TimeSpan TrialPeriod { get; protected init; }
}