using CorruptionClicker.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CorruptionClicker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] 
public class UserUpgradesController : ControllerBase
{
    private readonly AppDbContext _db;

    public UserUpgradesController(AppDbContext db)
    {
        _db = db;
    }

    public record UpsertUserUpgradeRequest(
        int UserId,
        int UpgradeId,
        int Quantity,
        long TotalSpent,
        bool IsEquipped
    );

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var query = _db.UserUpgrades
            .OrderBy(x => x.UserUpgradeId);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.UserUpgradeId,
                x.UserId,
                x.UpgradeId,
                x.Quantity,
                x.TotalSpent,
                x.IsEquipped,
                x.LastPurchasedAt
            })
            .ToListAsync();

        return Ok(new { page, pageSize, total, items });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var x = await _db.UserUpgrades.FirstOrDefaultAsync(u => u.UserUpgradeId == id);
        if (x is null) return NotFound();

        return Ok(new
        {
            x.UserUpgradeId,
            x.UserId,
            x.UpgradeId,
            x.Quantity,
            x.TotalSpent,
            x.IsEquipped,
            x.LastPurchasedAt
        });
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (userId <= 0) return BadRequest("userId is required");

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var query = _db.UserUpgrades
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.UserUpgradeId);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.UserUpgradeId,
                x.UserId,
                x.UpgradeId,
                x.Quantity,
                x.TotalSpent,
                x.IsEquipped
            })
            .ToListAsync();

        return Ok(new { page, pageSize, total, items });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertUserUpgradeRequest req)
    {
        if (req.UserId <= 0) return BadRequest("UserId must be > 0.");
        if (req.UpgradeId <= 0) return BadRequest("UpgradeId must be > 0.");
        if (req.Quantity < 0) return BadRequest("Quantity must be >= 0.");
        if (req.TotalSpent < 0) return BadRequest("TotalSpent must be >= 0.");

        var userExists = await _db.Users.AnyAsync(u => u.UserId == req.UserId);
        if (!userExists) return BadRequest("UserId not found.");

        var upgradeExists = await _db.Upgrades.AnyAsync(u => u.UpgradeId == req.UpgradeId);
        if (!upgradeExists) return BadRequest("UpgradeId not found.");

        var pairExists = await _db.UserUpgrades.AnyAsync(x => x.UserId == req.UserId && x.UpgradeId == req.UpgradeId);
        if (pairExists) return Conflict("This user already has a row for that upgrade.");

        var x = new UserUpgrade
        {
            UserId = req.UserId,
            UpgradeId = req.UpgradeId,
            Quantity = req.Quantity,
            TotalSpent = req.TotalSpent,
            IsEquipped = req.IsEquipped,
            LastPurchasedAt = DateTimeOffset.UtcNow
        };

        _db.UserUpgrades.Add(x);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = x.UserUpgradeId }, new { x.UserUpgradeId });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpsertUserUpgradeRequest req)
    {
        var x = await _db.UserUpgrades.FirstOrDefaultAsync(u => u.UserUpgradeId == id);
        if (x is null) return NotFound();

        if (req.UserId <= 0) return BadRequest("UserId must be > 0.");
        if (req.UpgradeId <= 0) return BadRequest("UpgradeId must be > 0.");
        if (req.Quantity < 0) return BadRequest("Quantity must be >= 0.");
        if (req.TotalSpent < 0) return BadRequest("TotalSpent must be >= 0.");

        var pairTaken = await _db.UserUpgrades.AnyAsync(y =>
            y.UserUpgradeId != id && y.UserId == req.UserId && y.UpgradeId == req.UpgradeId);

        if (pairTaken) return Conflict("Another row already uses that (userId, upgradeId) pair.");

        x.UserId = req.UserId;
        x.UpgradeId = req.UpgradeId;
        x.Quantity = req.Quantity;
        x.TotalSpent = req.TotalSpent;
        x.IsEquipped = req.IsEquipped;
        x.LastPurchasedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var x = await _db.UserUpgrades.FirstOrDefaultAsync(u => u.UserUpgradeId == id);
        if (x is null) return NotFound();

        _db.UserUpgrades.Remove(x);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}