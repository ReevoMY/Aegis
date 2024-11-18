using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;

namespace Sample.License.Web.Tests
{
    public class SampleLicenseServerDataSeedContributor : IDataSeedContributor, ITransientDependency
    {
        public Task SeedAsync(DataSeedContext context)
        {
            /* Seed additional test data... */

            return Task.CompletedTask;
        }
    }
}