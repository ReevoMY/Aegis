namespace Reevo.License.EntityFrameworkCore.Exceptions;

public class NotFoundException(string message) : ApiException(message, 404);