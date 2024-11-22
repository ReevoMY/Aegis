using Reevo.License.EntityFrameworkCore.Entities;
using Volo.Abp.Domain.Entities;

namespace Sample.License.Web.Entities;

/// <summary>
/// An example to show how to create a License entity that differs from the original <see cref="Reevo.License.EntityFrameworkCore.Entities.License"/> entity.
/// </summary>
public class SampleLicense : BasicAggregateRoot<Guid>
{

}