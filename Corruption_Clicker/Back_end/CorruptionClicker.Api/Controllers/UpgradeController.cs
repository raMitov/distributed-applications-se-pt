using CorruptionClicker.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CorruptionClicker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] 
public class UpgradesController : ControllerBase
{
    private readonly AppDbContext _db;

    public UpgradesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var query = _db.Upgrades
            .Where(u => u.IsActive)
            .OrderBy(u => u.UpgradeId);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.UpgradeId,
                u.Name,
                u.Description,
                u.ImageUrl,
                u.BaseCost,
                u.CpsBonus,
                u.CpcBonus,
                u.MaxQuantity,
                u.IsActive
            })
            .ToListAsync();

        return Ok(new { page, pageSize, total, items });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var u = await _db.Upgrades.FirstOrDefaultAsync(x => x.UpgradeId == id);
        if (u is null) return NotFound();

        return Ok(new
        {
            u.UpgradeId,
            u.Name,
            u.Description,
            u.ImageUrl,
            u.BaseCost,
            u.CpsBonus,
            u.CpcBonus,
            u.MaxQuantity,
            u.IsActive
        });
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string name, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        name = (name ?? "").Trim();
        if (name.Length == 0) return BadRequest("name is required");

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var query = _db.Upgrades
            .Where(u => u.IsActive && EF.Functions.ILike(u.Name, $"%{name}%"))
            .OrderBy(u => u.UpgradeId);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new { u.UpgradeId, u.Name, u.BaseCost })
            .ToListAsync();

        return Ok(new { page, pageSize, total, items });
    }

public record UpsertUpgradeRequest(
    string Name,
    string Description,
    string ImageUrl,
    int BaseCost,
    double CpsBonus,
    int CpcBonus,
    int MaxQuantity,
    bool IsActive
);

[HttpPost]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> Create([FromBody] UpsertUpgradeRequest req)
{
    if (string.IsNullOrWhiteSpace(req.Name) || req.Name.Length > 50)
        return BadRequest("Name is required and must be <= 50 chars.");
    if (string.IsNullOrWhiteSpace(req.Description) || req.Description.Length > 200)
        return BadRequest("Description is required and must be <= 200 chars.");
    if (string.IsNullOrWhiteSpace(req.ImageUrl) || req.ImageUrl.Length > 200)
        return BadRequest("ImageUrl is required and must be <= 200 chars.");
    if (req.BaseCost < 0) return BadRequest("BaseCost must be >= 0.");
    if (req.CpsBonus < 0) return BadRequest("CpsBonus must be >= 0.");
    if (req.CpcBonus < 0) return BadRequest("CpcBonus must be >= 0.");
    if (req.MaxQuantity < 0) return BadRequest("MaxQuantity must be >= 0.");

    var exists = await _db.Upgrades.AnyAsync(u => u.Name == req.Name);
    if (exists) return Conflict("An upgrade with this name already exists.");

    var upgrade = new Upgrade
    {
        Name = req.Name.Trim(),
        Description = req.Description.Trim(),
        ImageUrl = req.ImageUrl.Trim(),
        BaseCost = req.BaseCost,
        CpsBonus = req.CpsBonus,
        CpcBonus = req.CpcBonus,
        MaxQuantity = req.MaxQuantity,
        IsActive = req.IsActive
    };

    _db.Upgrades.Add(upgrade);
    await _db.SaveChangesAsync();

    return CreatedAtAction(nameof(GetById), new { id = upgrade.UpgradeId }, new
    {
        upgrade.UpgradeId,
        upgrade.Name
    });
}

[HttpPut("{id:int}")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpsertUpgradeRequest req)
{
    var upgrade = await _db.Upgrades.FirstOrDefaultAsync(u => u.UpgradeId == id);
    if (upgrade is null) return NotFound();

    if (string.IsNullOrWhiteSpace(req.Name) || req.Name.Length > 50)
        return BadRequest("Name is required and must be <= 50 chars.");
    if (string.IsNullOrWhiteSpace(req.Description) || req.Description.Length > 200)
        return BadRequest("Description is required and must be <= 200 chars.");
    if (string.IsNullOrWhiteSpace(req.ImageUrl) || req.ImageUrl.Length > 200)
        return BadRequest("ImageUrl is required and must be <= 200 chars.");
    if (req.BaseCost < 0) return BadRequest("BaseCost must be >= 0.");
    if (req.CpsBonus < 0) return BadRequest("CpsBonus must be >= 0.");
    if (req.CpcBonus < 0) return BadRequest("CpcBonus must be >= 0.");
    if (req.MaxQuantity < 0) return BadRequest("MaxQuantity must be >= 0.");

   
    var nameTaken = await _db.Upgrades.AnyAsync(u => u.Name == req.Name && u.UpgradeId != id);
    if (nameTaken) return Conflict("Another upgrade already has this name.");

    upgrade.Name = req.Name.Trim();
    upgrade.Description = req.Description.Trim();
    upgrade.ImageUrl = req.ImageUrl.Trim();
    upgrade.BaseCost = req.BaseCost;
    upgrade.CpsBonus = req.CpsBonus;
    upgrade.CpcBonus = req.CpcBonus;
    upgrade.MaxQuantity = req.MaxQuantity;
    upgrade.IsActive = req.IsActive;

    await _db.SaveChangesAsync();
    return NoContent();
}


[HttpDelete("{id:int}")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> Delete([FromRoute] int id)
{
    var upgrade = await _db.Upgrades.FirstOrDefaultAsync(u => u.UpgradeId == id);
    if (upgrade is null) return NotFound();

    _db.Upgrades.Remove(upgrade);
    await _db.SaveChangesAsync();

    return NoContent();
}

}