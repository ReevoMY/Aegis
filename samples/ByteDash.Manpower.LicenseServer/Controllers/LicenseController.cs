using Microsoft.AspNetCore.Mvc;
using Reevo.License.Domain.Shared.Enum;
using Volo.Abp.AspNetCore.Mvc;

namespace ByteDash.Manpower.LicenseServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LicenseController : AbpController
{
    private static readonly List<ManpowerLicense> Licenses =
    [
        new(Guid.Parse("637e6fda-f081-4bc6-83bd-68a0a7a1b5c8"), LicenseType.Standard) { Description = "Standard License" },
        new(Guid.Parse("1024cffb-efde-402e-98d5-98ab5fdb3118"), LicenseType.Trial) { Description = "Trial License" }
    ];

    [HttpGet]
    public ActionResult<IEnumerable<ManpowerLicense>> Get()
    {
        return Ok(Licenses);
    }

    [HttpGet("{id}")]
    public ActionResult<ManpowerLicense> Get(Guid id)
    {
        var license = Licenses.FirstOrDefault(l => l.Id == id);
        if (license == null)
        {
            return NotFound();
        }
        return Ok(license);
    }

    [HttpPost]
    public ActionResult<ManpowerLicense> Post([FromBody] ManpowerLicense license)
    {
        Licenses.Add(license);
        return CreatedAtAction(nameof(Get), new { id = license.Id }, license);
    }

    [HttpPut("{id}")]
    public ActionResult Put(Guid id, [FromBody] ManpowerLicense license)
    {
        var existingLicense = Licenses.FirstOrDefault(l => l.Id == id);
        if (existingLicense == null)
        {
            return NotFound();
        }
        return NoContent();
    }

    [HttpDelete("{id}")]
    public ActionResult Delete(Guid id)
    {
        var license = Licenses.FirstOrDefault(l => l.Id == id);
        if (license == null)
        {
            return NotFound();
        }
        Licenses.Remove(license);
        return NoContent();
    }
}