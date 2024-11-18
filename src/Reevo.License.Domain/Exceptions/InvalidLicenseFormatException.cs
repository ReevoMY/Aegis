namespace Reevo.License.Domain.Exceptions;

public class InvalidLicenseFormatException(string message) : LicenseValidationException(message);