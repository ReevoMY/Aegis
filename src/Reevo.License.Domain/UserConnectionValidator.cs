using Volo.Abp.Domain.Services;

namespace Reevo.License.Domain;

public class UserConnectionValidator : DomainService
{
    #region Fields

    private readonly HttpClient _httpClient;

    #endregion

    public UserConnectionValidator(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> IsConnectionValidAsync(string ipAddress)
    {
        try
        {
            var response = await _httpClient.GetAsync($"http://{ipAddress}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}