using Aegis.Server.AspNetCore.Localization;
using FluentAssertions;
using Microsoft.Extensions.Localization;

namespace Aegis.Server.AspNetCore.Tests.Localization;

public class FeaturesLocalizationTests : SampleLicenseServerTestBase
{
    #region Fields

    private readonly IStringLocalizer<FeaturesResource> _localizer;

    #endregion

    public FeaturesLocalizationTests()
    {
        _localizer = GetRequiredService<IStringLocalizer<FeaturesResource>>();
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