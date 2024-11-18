namespace Reevo.License.EntityFrameworkCore.Exceptions;

public class BadRequestException(string message) : ApiException(message, 400);