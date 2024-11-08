using System.Globalization;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Aegis.Enums;
using Aegis.Exceptions;
using Aegis.Models;
using Aegis.Utilities;
using Ardalis.GuardClauses;

[assembly: InternalsVisibleTo("Aegis.Tests")]

namespace Aegis;

public static class LicenseManager
{
    #region Fields

    // Validation Endpoint
    private static readonly HttpClient HttpClient = new();
    private static string _serverBaseEndpoint = "https://your-api-url/api/licenses";
    private static readonly string ValidationEndpoint = $"{_serverBaseEndpoint}/validate";
    private static readonly string HeartbeatEndpoint = $"{_serverBaseEndpoint}/heartbeat";
    private static readonly string DisconnectEndpoint = $"{_serverBaseEndpoint}/disconnect";
    private static TimeSpan _heartbeatInterval = TimeSpan.FromMinutes(5); // Should be less than server timeout
    private static Timer? _heartbeatTimer;

    #endregion

    public static BaseLicense? Current { get; private set; }

    #region Methods

    #region Public

    /// <summary>
    ///     Sets the base endpoint for the licensing server.
    /// </summary>
    /// <param name="serverBaseEndpoint">The base endpoint for the licensing server.</param>
    /// <exception cref="ArgumentNullException">Thrown if the server base endpoint is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the server base endpoint is empty.</exception>
    public static void SetServerBaseEndpoint(string serverBaseEndpoint)
    {
        switch (serverBaseEndpoint)
        {
            case null:
                throw new ArgumentNullException(nameof(serverBaseEndpoint));
            case "":
                throw new ArgumentException("Server base endpoint cannot be empty.", nameof(serverBaseEndpoint));
        }

        if (serverBaseEndpoint.EndsWith('/'))
        {
            serverBaseEndpoint = serverBaseEndpoint[..^1];
        }
        _serverBaseEndpoint = serverBaseEndpoint;
    }

    /// <summary>
    ///     Sets the heartbeat interval for concurrent licenses.
    /// </summary>
    /// <param name="heartbeatInterval">The heartbeat interval.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the heartbeat interval is negative.</exception>
    public static void SetHeartbeatInterval(TimeSpan heartbeatInterval)
    {
        if (heartbeatInterval < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(heartbeatInterval), "Heartbeat interval cannot be negative.");

        _heartbeatInterval = heartbeatInterval;
    }

    /// <summary>
    ///     Saves a license to a byte array or to a file.
    /// </summary>
    /// <typeparam name="T">The type of license to save.</typeparam>
    /// <param name="license">The license object to save.</param>
    /// <param name="filePath">The path to the file to save the license to.</param>
    /// <param name="privateKey">The private key for signing.</param>
    /// <returns>A byte array containing the encrypted and signed license data.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the license is null.</exception>
    public static byte[] SaveLicense<T>(T license, string? filePath = null, string? privateKey = null) where T : BaseLicense
    {
        Guard.Against.Null(license);

        // Generate the file bytes for the license object
        var licenseData = JsonSerializer.SerializeToUtf8Bytes(license, new JsonSerializerOptions { WriteIndented = true });
        var aesKey = SecurityUtils.GenerateAesKey();
        var encryptedData = SecurityUtils.EncryptData(licenseData, aesKey);
        var hash = SecurityUtils.CalculateSha256Hash(encryptedData);
        var signature = SecurityUtils.SignData(hash, privateKey ?? LicenseUtils.GetLicensingSecrets().PrivateKey);
        var combinedLicenseData = CombineLicenseData(hash, signature, encryptedData, aesKey);

        // Output to file if available
        if (filePath != null)
        {
            Guard.Against.NullOrEmpty(filePath, exceptionCreator: () => new DirectoryNotFoundException($"{filePath} is not a valid path."));
            File.WriteAllBytes(filePath, combinedLicenseData);
        }

        return combinedLicenseData;
    }

    /// <summary>
    ///     Loads a license from a file.
    /// </summary>
    /// <param name="filePath">The path to the file containing the license data.</param>
    /// <param name="validationMode">The validation mode to use (Online or Offline).</param>
    /// <returns>The loaded license object, or null if the license is invalid.</returns>
    /// <exception cref="InvalidLicenseSignatureException">Thrown if the license signature is invalid.</exception>
    /// <exception cref="InvalidLicenseFormatException">Thrown if the license file format is invalid.</exception>
    /// <exception cref="LicenseValidationException">Thrown if the license validation fails.</exception>
    public static async Task<BaseLicense?> LoadLicenseAsync(
        string filePath,
        ValidationMode validationMode = ValidationMode.Offline)
    {
        // Read the combined data from the file
        var licenseData = await File.ReadAllBytesAsync(filePath);

        // Load the license from the combined data
        Current = await LoadLicenseAsync(licenseData, validationMode);

        return Current;
    }

    /// <summary>
    ///     Loads a license from a byte array.
    /// </summary>
    /// <param name="licenseData">The byte array containing the license data.</param>
    /// <param name="validationMode">The validation mode to use (Online or Offline).</param>
    /// <param name="validationParams">Optional validation parameters.</param>
    /// <returns>The loaded license object, or null if the license is invalid.</returns>
    /// <exception cref="InvalidLicenseSignatureException">Thrown if the license signature is invalid.</exception>
    /// <exception cref="InvalidLicenseFormatException">Thrown if the license file format is invalid.</exception>
    /// <exception cref="LicenseValidationException">Thrown if the license validation fails.</exception>
    public static async Task<BaseLicense?> LoadLicenseAsync(
        byte[] licenseData,
        ValidationMode validationMode = ValidationMode.Offline,
        Dictionary<string, string?>? validationParams = null)
    {
        if (!LicenseValidator.VerifyLicenseData(licenseData, out var license) || license == null)
        {
            throw new InvalidLicenseFormatException("Invalid license file format.");
        }

        // Set the current license based on type
        license = license.Type switch
        {
            LicenseType.Standard => license as StandardLicense,
            LicenseType.Trial => license as TrialLicense,
            LicenseType.NodeLocked => license as NodeLockedLicense,
            LicenseType.Subscription => license as SubscriptionLicense,
            LicenseType.Concurrent => license as ConcurrentLicense,
            LicenseType.Floating => license as FloatingLicense,
            _ => license
        };

        // Validate the license, VerifyLicenseData will be called again, but it's mandatory to keep LicenseValidator independent.
        await ValidateLicenseAsync(license!, licenseData, validationMode, validationParams ?? GetValidationParams(license!)!);

        return license;
    }

    /// <summary>
    ///     Checks if a feature is enabled in the current license.
    /// </summary>
    /// <param name="featureName">The name of the feature to check.</param>
    /// <returns>True if the feature is enabled, false otherwise.</returns>
    public static bool IsFeatureEnabled(string featureName)
    {
        Guard.Against.NullOrEmpty(featureName);
        Guard.Against.Null(Current, exceptionCreator: () => new InvalidLicenseFormatException("Invalid license file format."));

        return Current.Features.ContainsKey(featureName) && Current.Features[featureName];
    }

    /// <summary>
    ///     Throws an exception if a feature is not allowed in the current license.
    /// </summary>
    /// <param name="featureName">The name of the feature to check.</param>
    /// <exception cref="FeatureNotLicensedException">Thrown if the feature is not allowed.</exception>
    public static void ThrowIfNotAllowed(string featureName)
    {
        if (!IsFeatureEnabled(featureName))
            throw new FeatureNotLicensedException($"Feature '{featureName}' is not allowed in your licensing model.");
    }

    /// <summary>
    ///     Closes connection to the licensing server and releases any resources.
    /// </summary>
    public static async Task CloseAsync()
    {
        if (_heartbeatTimer != null)
        {
            await _heartbeatTimer.DisposeAsync();
            _heartbeatTimer = null;
        }

        if (Current is { Type: LicenseType.Concurrent })
            await SendDisconnectAsync();

        HttpClient.Dispose();
        Current = null;
    }

    #endregion

    #region Private

    /// <summary>
    ///     Validates the license asynchronously.
    /// </summary>
    /// <param name="license">The license object to validate.</param>
    /// <param name="licenseData">The raw license data.</param>
    /// <param name="validationMode">The validation mode to use (Online or Offline).</param>
    /// <param name="validationParams">Optional validation parameters.</param>
    /// <returns>A task that represents the asynchronous validation operation.</returns>
    /// <exception cref="LicenseValidationException">Thrown if the license validation fails.</exception>
    private static async Task ValidateLicenseAsync(BaseLicense license, byte[] licenseData,
        ValidationMode validationMode, Dictionary<string, string?>? validationParams)
    {
        switch (validationMode)
        {
            case ValidationMode.Online:
                await ValidateLicenseOnlineAsync(license, licenseData);
                break;
            case ValidationMode.Offline:
                ValidateLicenseOffline(license, licenseData, validationParams);
                break;
        }

        if (license.Type == LicenseType.Concurrent)
        {
            _heartbeatTimer ??= new Timer(state => { _ = SendHeartbeat(); }, null, TimeSpan.Zero, _heartbeatInterval);
        }
    }

    /// <summary>
    ///     Validates the license online.
    /// </summary>
    /// <param name="license">The license object to validate.</param>
    /// <param name="licenseData">The raw license data.</param>
    /// <param name="validationParams">Optional validation parameters.</param>
    /// <returns>A task that represents the asynchronous validation operation.</returns>
    /// <exception cref="LicenseValidationException">Thrown if the online validation fails.</exception>
    private static async Task ValidateLicenseOnlineAsync(BaseLicense license, byte[] licenseData,
        Dictionary<string, string?>? validationParams = null)
    {
        // Prepare the request content 
        var formData = new MultipartFormDataContent();
        formData.Add(new StringContent(license.LicenseKey), "licenseKey");
        var validationContent = GetValidationParams(license, validationParams);
        formData.Add(new StringContent(JsonSerializer.Serialize(validationContent)), "validationParams");
        formData.Add(new ByteArrayContent(licenseData), "licenseFile", "license.bin");

        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("X-API-KEY", LicenseUtils.GetLicensingSecrets().ApiKey);

        var response = await HttpClient.PostAsync(ValidationEndpoint, formData);
        if (!response.IsSuccessStatusCode)
        {
            throw new LicenseValidationException(
                $"Online validation failed: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        }
    }

    /// <summary>
    ///     Validates the license offline.
    /// </summary>
    /// <param name="license">The license object to validate.</param>
    /// <param name="licenseData">The raw license data.</param>
    /// <param name="validationParams">Optional validation parameters.</param>
    /// <exception cref="LicenseValidationException">Thrown if the offline validation fails.</exception>
    private static void ValidateLicenseOffline(BaseLicense license, byte[] licenseData,
        Dictionary<string, string?>? validationParams = null)
    {
        var isLicenseValid = license.Type switch
        {
            LicenseType.Standard => LicenseValidator.ValidateStandardLicense(
                licenseData,
                validationParams?["UserName"]!, validationParams?["SerialNumber"]!),
            LicenseType.Trial => LicenseValidator.ValidateTrialLicense(licenseData),
            LicenseType.NodeLocked => LicenseValidator.ValidateNodeLockedLicense(
                licenseData,
                validationParams?["HardwareId"]),
            LicenseType.Subscription => LicenseValidator.ValidateSubscriptionLicense(licenseData),
            LicenseType.Floating => LicenseValidator.ValidateFloatingLicense(
                licenseData,
                validationParams?["UserName"]!, int.Parse(validationParams?["MaxActiveUsersCount"]!)),
            LicenseType.Concurrent => LicenseValidator.ValidateConcurrentLicense(
                licenseData,
                validationParams?["UserName"]!, int.Parse(validationParams?["MaxActiveUsersCount"]!)),
            _ => false
        };

        if (isLicenseValid)
        {
            isLicenseValid = LicenseValidator.ValidateLicenseRules(license, validationParams);
        }

        if (!isLicenseValid)
        {
            throw new LicenseValidationException("License validation failed.");
        }
    }

    /// <summary>
    ///     Gets the validation parameters for the license.
    /// </summary>
    /// <param name="license">The license object.</param>
    /// <param name="validationParams">Optional validation parameters.</param>
    /// <returns>A dictionary of validation parameters.</returns>
    private static Dictionary<string, string> GetValidationParams(
        BaseLicense? license,
        Dictionary<string, string?>? validationParams = null)
    {
        return license switch
        {
            StandardLicense standardLicense => new Dictionary<string, string>
            {
                { "UserName", validationParams?["UserName"] ?? standardLicense.UserName },
                { "SerialNumber", validationParams?["SerialNumber"] ?? standardLicense.LicenseKey }
            },
            NodeLockedLicense nodeLockedLicense => new Dictionary<string, string>
            {
                { "HardwareId", validationParams?["HardwareId"] ?? nodeLockedLicense.HardwareId }
            },
            SubscriptionLicense subscriptionLicense => new Dictionary<string, string>
            {
                { "UserName", subscriptionLicense.UserName },
                {
                    "SubscriptionStartDate",
                    subscriptionLicense.SubscriptionStartDate.ToString(CultureInfo.InvariantCulture)
                },
                { "SubscriptionDuration", subscriptionLicense.SubscriptionDuration.ToString() }
            },
            FloatingLicense floatingLicense => new Dictionary<string, string>
            {
                { "UserName", floatingLicense.UserName },
                { "MaxActiveUsersCount", floatingLicense.MaxActiveUsersCount.ToString() }
            },
            ConcurrentLicense concurrentLicense => new Dictionary<string, string>
            {
                { "UserName", validationParams?["UserName"] ?? concurrentLicense.UserName },
                { "MaxActiveUsersCount", concurrentLicense.MaxActiveUsersCount.ToString() }
            },
            TrialLicense trialLicense => new Dictionary<string, string>
            {
                { "TrialPeriod", trialLicense.TrialPeriod.ToString() }
            },
            _ => new Dictionary<string, string>()
        };
    }

    /// <summary>
    ///     Sends a heartbeat to the licensing server.
    /// </summary>
    /// <returns>A task that represents the asynchronous heartbeat operation.</returns>
    /// <exception cref="HeartbeatException">Thrown if the heartbeat fails.</exception>
    private static async Task SendHeartbeat()
    {
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("X-API-KEY", LicenseUtils.GetLicensingSecrets().ApiKey);
        var response = await HttpClient.PostAsync(HeartbeatEndpoint, null);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new HeartbeatException(
                $"Heartbeat failed: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        }
    }

    /// <summary>
    ///     Sends a disconnect message to the licensing server.
    /// </summary>
    /// <returns>A task that represents the asynchronous disconnect operation.</returns>
    /// <exception cref="HeartbeatException">Thrown if the disconnect fails.</exception>
    private static async Task SendDisconnectAsync()
    {
        HttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("X-API-KEY", LicenseUtils.GetLicensingSecrets().ApiKey);
        var response = await HttpClient.PostAsync(DisconnectEndpoint, null);
        if (!response.IsSuccessStatusCode)
            throw new HeartbeatException(
                $"Concurrent user disconnect failed: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
    }

    private static byte[] CombineLicenseData(byte[] hash, byte[] signature, byte[] encryptedData, byte[] aesKey)
    {
        var combinedData = new byte[
            4 + hash.Length + // Length of hash + hash data
            4 + signature.Length + // Length of signature + signature data
            4 + encryptedData.Length + // Length of encrypted data + data
            4 + aesKey.Length // Length of AES key + key data
        ];

        var offset = 0;

        // Copy hash length and hash data
        var hashLengthBytes = BitConverter.GetBytes(hash.Length);
        Array.Copy(hashLengthBytes, 0, combinedData, offset, 4);
        offset += 4;
        Array.Copy(hash, 0, combinedData, offset, hash.Length);
        offset += hash.Length;

        // Copy signature length and signature data
        var signatureLengthBytes = BitConverter.GetBytes(signature.Length);
        Array.Copy(signatureLengthBytes, 0, combinedData, offset, 4);
        offset += 4;
        Array.Copy(signature, 0, combinedData, offset, signature.Length);
        offset += signature.Length;

        // Copy encrypted data length and encrypted data
        var encryptedDataLengthBytes = BitConverter.GetBytes(encryptedData.Length);
        Array.Copy(encryptedDataLengthBytes, 0, combinedData, offset, 4);
        offset += 4;
        Array.Copy(encryptedData, 0, combinedData, offset, encryptedData.Length);
        offset += encryptedData.Length;

        // Copy AES key length and AES key data
        var aesKeyLengthBytes = BitConverter.GetBytes(aesKey.Length);
        Array.Copy(aesKeyLengthBytes, 0, combinedData, offset, 4);
        offset += 4;
        Array.Copy(aesKey, 0, combinedData, offset, aesKey.Length);

        return combinedData;
    }

    #endregion

    #endregion
}