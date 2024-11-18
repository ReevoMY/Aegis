namespace Reevo.License.EntityFrameworkCore.Exceptions;

public class UnauthorizedAccessException(string message) : ApiException(message, 401);