namespace Reevo.License.Domain.Exceptions;

public class HardwareMismatchException(string message) : LicenseValidationException(message);