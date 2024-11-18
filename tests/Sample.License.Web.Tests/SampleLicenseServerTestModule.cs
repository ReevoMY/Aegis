using Reevo.License.TestBase;
using Volo.Abp.Modularity;

namespace Aegis.Server.AspNetCore.Tests;

[DependsOn(
    typeof(SampleLicenseServerMvcModule),
    typeof(LicenseTestBaseModule)
)]
public class SampleLicenseServerTestModule : AbpModule
{
}