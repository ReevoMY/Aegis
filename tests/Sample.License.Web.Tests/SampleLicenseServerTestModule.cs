using Sample.License.Web;
using Reevo.License.TestBase;
using Volo.Abp.Modularity;

namespace Sample.License.Web.Tests;

[DependsOn(
    typeof(SampleLicenseServerMvcModule),
    typeof(LicenseTestBaseModule)
)]
public class SampleLicenseServerTestModule : AbpModule
{
}