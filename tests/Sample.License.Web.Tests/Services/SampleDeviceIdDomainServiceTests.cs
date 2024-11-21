using DeviceId;
using DeviceId.Encoders;
using FluentAssertions;
using Reevo.License.Domain.Shared.Service;
using Sample.License.Web.Services;

namespace Sample.License.Web.Tests.Services;

public class SampleDeviceIdDomainServiceTests //: SampleLicenseServerTestBase
{
    private readonly IDeviceIdDomainService _sut = new SampleDeviceIdDomainService(new SampleDeviceIdFormatter());

    #region GetDeviceIdAsync

    [Fact]
    public async Task GetDeviceIdAsync_ShouldReturnValidDeviceIdString()
    {
        // Act
        var actual = await _sut.GetDeviceIdAsync();

        // Assert
        // https://github.com/MatthewKing/DeviceId/issues/68
        actual.Should().NotBeEmpty();
        actual.Length.Should().Be(53); // 52 Crockford + 1 checksum
        actual.All(c => Base32ByteArrayEncoder.CrockfordAlphabet.Contains(c)).Should().BeTrue();
    }

    #endregion

    #region VerifyDeviceIdAsync

    [Fact]
    public async Task VerifyDeviceIdAsync_ValidDeviceId_ShouldReturnTrue()
    {
        // Arrange
        var deviceId = await _sut.GetDeviceIdAsync();

        // Act
        var actual = await _sut.VerifyDeviceIdAsync(deviceId);

        // Assert
        actual.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyDeviceIdAsync_InvalidDeviceId_ShouldReturnFalse()
    {
        // Arrange
        var deviceId = GetInvalidDeviceId();

        // Act
        var actual = await _sut.VerifyDeviceIdAsync(deviceId);

        // Assert
        actual.Should().BeFalse();
    }

    #endregion

    #region Private

    private string GetInvalidDeviceId()
    {
        var deviceId = new DeviceIdBuilder()
            .AddMachineName()
            .AddMacAddress(true, true)
            .AddFileToken(SampleDeviceIdDomainService.FileToken)
            .ToString();

        return deviceId;
    }

    #endregion
}