using Reevo.License.Domain.Models;
using Reevo.License.Domain.Models.Utils;

namespace Reevo.License.Domain.Interfaces;

public interface IValidationRule
{
    LicenseValidationResult<T> Validate<T>(T license, Dictionary<string, string?>? parameters) where T : BaseLicense;
}