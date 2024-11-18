namespace Reevo.License.Domain.Exceptions;

public class HeartbeatException(string message) : LicenseValidationException(message);