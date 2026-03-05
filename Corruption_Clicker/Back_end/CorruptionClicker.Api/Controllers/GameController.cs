using CorruptionClicker.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CorruptionClicker.Api.Controllers;

[ApiController]
[Route("api/game")]
[Authorize]
public class GameController : ControllerBase
{
    private readonly AppDbContext _db;

    public GameController(AppDbContext db)
    {
        _db = db;
    }

    private int GetUserId()
    {
        return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

   
    //Returns the full game state for the logged in user
    [HttpGet("state")]
    public async Task<IActionResult> GetGameState()
    {
        var userId = GetUserId();

        var user = await _db.Users.FirstAsync(u => u.UserId == userId);

        var upgrades = await _db.Upgrades
            .Where(u => u.IsActive)
            .ToListAsync();

        var owned = await _db.UserUpgrades
            .Where(x => x.UserId == userId)
            .ToListAsync();

        var result = upgrades.Select(u =>
        {
            var ownedUpgrade = owned.FirstOrDefault(o => o.UpgradeId == u.UpgradeId);

            int quantity = ownedUpgrade?.Quantity ?? 0;

            double nextCost = u.BaseCost * Math.Pow(1.15, quantity);

            return new
            {
                u.UpgradeId,
                u.Name,
                u.Description,
                u.ImageUrl,
                u.CpsBonus,
                u.CpcBonus,
                Quantity = quantity,
                NextCost = (int)nextCost
            };
        });

        return Ok(new
        {
            user.CashBalance,
            user.CashPerClick,
            Upgrades = result
        });
    }

    [HttpPost("click")]
    public async Task<IActionResult> Click_Mr_Cash()
    {
        var userId = GetUserId();

        var user = await _db.Users.FirstAsync(u => u.UserId == userId);

        user.CashBalance += user.CashPerClick;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            user.CashBalance
        });
    }

    // Buy an upgrade
    [HttpPost("buy/{upgradeId}")]
    public async Task<IActionResult> BuyUpgrade(int upgradeId)
    {
        var userId = GetUserId();

        var user = await _db.Users.FirstAsync(u => u.UserId == userId);

        var upgrade = await _db.Upgrades.FirstOrDefaultAsync(u => u.UpgradeId == upgradeId);

        if (upgrade == null || !upgrade.IsActive)
            return BadRequest("Upgrade not available");

        var owned = await _db.UserUpgrades
            .FirstOrDefaultAsync(x => x.UserId == userId && x.UpgradeId == upgradeId);

        int quantity = owned?.Quantity ?? 0;

        double cost = upgrade.BaseCost * Math.Pow(1.15, quantity);

        if (user.CashBalance < cost)
            return BadRequest("Not enough cash");

        user.CashBalance -= (int)cost;

        if (owned == null)
        {
            owned = new UserUpgrade
            {
                UserId = userId,
                UpgradeId = upgradeId,
                Quantity = 1,
                TotalSpent = (long)cost,
                IsEquipped = true,
                LastPurchasedAt = DateTimeOffset.UtcNow
            };

            _db.UserUpgrades.Add(owned);
        }
        else
        {
            owned.Quantity += 1;
            owned.TotalSpent += (long)cost;
            owned.LastPurchasedAt = DateTimeOffset.UtcNow;
        }

        user.CashPerClick += upgrade.CpcBonus;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            user.CashBalance,
            owned.Quantity
        });
    }

    [HttpPost("tick")]
[Authorize]
public async Task<IActionResult> Tick()
{
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    var user = await _db.Users.FindAsync(userId);
    if (user == null)
        return NotFound();

    var totalCps = await _db.UserUpgrades
        .Where(x => x.UserId == userId)
        .Join(_db.Upgrades,
              uu => uu.UpgradeId,
              u => u.UpgradeId,
              (uu, u) => new { uu.Quantity, u.CpsBonus })
        .SumAsync(x => x.Quantity * x.CpsBonus);

    user.CashBalance += (long)totalCps;

    await _db.SaveChangesAsync();

    return Ok(new
    {
        balance = user.CashBalance,
        cps = totalCps
    });
}
}