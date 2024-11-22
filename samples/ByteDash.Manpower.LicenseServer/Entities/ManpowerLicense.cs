using Reevo.License.Domain.Shared.Enum;
using Reevo.License.EntityFrameworkCore.Entities;

namespace ByteDash.Manpower.LicenseServer;

public class ManpowerLicense : License
{
    public ManpowerLicense() : base()
    {
    }

    public ManpowerLicense(Guid id, LicenseType type) : base(id, type)
    {
    }
}