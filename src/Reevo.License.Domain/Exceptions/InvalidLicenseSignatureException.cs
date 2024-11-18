namespace Reevo.License.Domain.Exceptions;

public class InvalidLicenseSignatureException(string message) : LicenseValidationException(message);