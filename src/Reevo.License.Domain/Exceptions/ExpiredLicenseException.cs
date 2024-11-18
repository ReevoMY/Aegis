namespace Reevo.License.Domain.Exceptions;

public class ExpiredLicenseException(string message) : LicenseValidationException(message);