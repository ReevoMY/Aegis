using ByteDash.Manpower.LicenseServer.Data;
using ByteDash.Manpower.LicenseServer.Entities.License;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.AspNetCore.Mvc;

namespace ByteDash.Manpower.LicenseServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LicenseController : AbpController
{
    private readonly ManpowerLicenseServerDbContext _context;

    public LicenseController(ManpowerLicenseServerDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ManpowerLicense>>> GetLicenses()
    {
        return await _context.Licenses.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ManpowerLicense>> GetLicense(Guid id)
    {
        var license = await _context.Licenses.FindAsync(id);
        if (license == null)
        {
            return NotFound();
        }
        return license;
    }

    [HttpPost]
    public async Task<ActionResult<ManpowerLicense>> PostLicense(ManpowerLicense license)
    {
        _context.Licenses.Add(license);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetLicense), new { id = license.Id }, license);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutLicense(Guid id, ManpowerLicense license)
    {
        if (id != license.Id)
        {
            return BadRequest();
        }

        _context.Entry(license).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLicense(Guid id)
    {
        var license = await _context.Licenses.FindAsync(id);
        if (license == null)
        {
            return NotFound();
        }

        _context.Licenses.Remove(license);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}