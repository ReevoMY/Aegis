using DeviceId;
using DeviceId.Encoders;
using FluentAssertions;
using Sample.License.Web.Services;

namespace Sample.License.Web.Tests.Services;

public class SampleDeviceIdFormatterTests //: SampleLicenseServerTestBase
{
    private readonly IDeviceIdFormatter _sut = new SampleDeviceIdFormatter();

    #region GetDeviceId

    [Fact]
    public Task GetDeviceId_ShouldReturnValidDeviceIdString()
    {
        // Arrange
        var deviceId = new DeviceIdBuilder()
            .AddUserName()
            .AddMachineName()
            .AddOsVersion();

        // Act
        var result = _sut.GetDeviceId(deviceId.Components);

        // Assert
        // https://github.com/MatthewKing/DeviceId/issues/68
        result.Should().NotBeEmpty();
        result.Length.Should().Be(53); // 52 Crockford + 1 checksum
        result.All(c => Base32ByteArrayEncoder.CrockfordAlphabet.Contains(c)).Should().BeTrue();

        return Task.CompletedTask;
    }

    [Fact]
    public Task GetDeviceId_WithNullDeviceId_ShouldThrowInvalidOperationException()
    {
        // Act
        var act = () => _sut.GetDeviceId(null);

        // Assert
        act.Should().Throw<InvalidOperationException>();

        return Task.CompletedTask;
    }

    [Fact]
    public Task GetDeviceId_WithEmptyDeviceId_ShouldThrowInvalidOperationException()
    {
        // Act
        var act = () => _sut.GetDeviceId(new Dictionary<string, IDeviceIdComponent>());

        // Assert
        act.Should().Throw<InvalidOperationException>();

        return Task.CompletedTask;
    }

    #endregion
}