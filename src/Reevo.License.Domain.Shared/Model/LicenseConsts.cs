namespace Reevo.License.Domain.Shared.Model;

public class LicenseConsts
{
    public const int MaxLicenseKeyLength = 512;
    public const string LicenseKeyDataType = "nvarchar(512)";

    public const int MaxDescriptionLength = 512;
    public const string DescriptionDataType = "nvarchar(512)";

    public const int MaxIssuerLength = 256;
    public const string IssuerDataType = "nvarchar(256)";

    public const int MaxIssuedToLength = 256;
    public const string IssuedToDataType = "nvarchar(256)";

    public const int MaxVersionLength = 64;
    public const string VersionDataType = "nvarchar(64)";

    public const int MaxDeviceIdLength = 64;
    public const string DeviceIdDataType = "nvarchar(52)";
}