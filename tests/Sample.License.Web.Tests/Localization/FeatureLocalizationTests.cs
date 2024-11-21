using Sample.License.Web.Localization;
using FluentAssertions;
using Microsoft.Extensions.Localization;

namespace Sample.License.Web.Tests.Localization;

public class FeatureLocalizationTests : SampleLicenseServerTestBase
{
    #region Fields

    private readonly IStringLocalizer<FeatureResource> _localizer;

    #endregion

    public FeatureLocalizationTests()
    {
        _localizer = GetRequiredService<IStringLocalizer<FeatureResource>>();
    }

    [Fact]
    public Task Test_Localize_Product1()
    {
        _localizer["Product1"].Value.Should().Be("Product 1");
        _localizer["Product1.Feature1"].Value.Should().Be("P1.F1");
        _localizer["Product1.Feature1.Child1"].Value.Should().Be("P1.F1.C1");

        return Task.CompletedTask;
    }
}