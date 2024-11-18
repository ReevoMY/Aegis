namespace Reevo.License.Domain.Exceptions;

public class UserMismatchException(string message) : LicenseValidationException(message);