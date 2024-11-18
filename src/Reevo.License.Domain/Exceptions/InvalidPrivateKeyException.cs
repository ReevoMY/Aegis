namespace Reevo.License.Domain.Exceptions;

public class InvalidPrivateKeyException(string message) : LicenseGenerationException(message);