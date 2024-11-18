namespace Reevo.License.Domain.Exceptions;

public class MaximumActivationsReachedException(string message) : LicenseException(message);