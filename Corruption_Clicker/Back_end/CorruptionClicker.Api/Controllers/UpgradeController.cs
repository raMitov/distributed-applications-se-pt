using CorruptionClicker.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CorruptionClicker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // requires JWT for all endpoints here
public class UpgradesController : ControllerBase
{
    private readonly AppDbContext _db;

    public UpgradesController(AppDbContext db)
    {
        _db = db;
    }

    // GET /api/upgrades?page=1&pageSize=10
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

    // GET /api/upgrades/5
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

    // GET /api/upgrades/search?name=cur&page=1&pageSize=10
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
}