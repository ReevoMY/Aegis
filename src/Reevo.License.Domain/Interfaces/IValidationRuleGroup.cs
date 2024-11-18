using Reevo.License.Domain.Models;
using Reevo.License.Domain.Models.Utils;

namespace Reevo.License.Domain.Interfaces;

public interface IValidationRuleGroup
{
    IEnumerable<IValidationRule> Rules { get; }
    
    LicenseValidationResult<T> Validate<T>(T license, Dictionary<string, string?>? validationParams = null) where T : BaseLicense;
}