using CorruptionClicker.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CorruptionClicker.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")] // Only admins can manage users
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    public record CreateUserRequest(string UserName, string Email, string Password, string Role);
    public record UpdateUserRequest(string UserName, string Email, string Role);

    [HttpGet]
    public async Task<IActionResult> ListUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var query = _db.Users.OrderBy(u => u.UserId);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.UserId,
                u.UserName,
                u.Email,
                u.Role,
                u.CashBalance,
                u.CashPerClick,
                u.CreatedAt
            })
            .ToListAsync();

        return Ok(new { page, pageSize, total, items });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUserById([FromRoute] int id)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.UserId == id);
        if (u is null) return NotFound();

        return Ok(new
        {
            u.UserId,
            u.UserName,
            u.Email,
            u.Role,
            u.CashBalance,
            u.CashPerClick,
            u.CreatedAt
        });
    }
    
    [HttpGet("search")]
    public async Task<IActionResult> SearchUsersByUsername(
        [FromQuery] string username,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        username = (username ?? "").Trim();
        if (username.Length == 0) return BadRequest("username is required");

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var query = _db.Users
            .Where(u => EF.Functions.ILike(u.UserName, $"%{username}%"))
            .OrderBy(u => u.UserId);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new { u.UserId, u.UserName, u.Email, u.Role })
            .ToListAsync();

        return Ok(new { page, pageSize, total, items });
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest req)
    {
        // Keep validation aligned with DB constraints
        if (string.IsNullOrWhiteSpace(req.UserName) || req.UserName.Length > 30)
            return BadRequest("UserName is required and must be <= 30 chars.");
        if (string.IsNullOrWhiteSpace(req.Email) || req.Email.Length > 100)
            return BadRequest("Email is required and must be <= 100 chars.");
        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 6)
            return BadRequest("Password must be at least 6 chars.");
        var role = (req.Role ?? "User").Trim();
        if (role.Length == 0 || role.Length > 20) return BadRequest("Role must be <= 20 chars.");

        var exists = await _db.Users.AnyAsync(u => u.UserName == req.UserName || u.Email == req.Email);
        if (exists) return Conflict("Username or email already exists.");

        var user = new User
        {
            UserName = req.UserName.Trim(),
            Email = req.Email.Trim(),
            Role = role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            CashBalance = 0,
            CashPerClick = 1,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUserById), new { id = user.UserId }, new
        {
            user.UserId,
            user.UserName,
            user.Email,
            user.Role
        });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser([FromRoute] int id, [FromBody] UpdateUserRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == id);
        if (user is null) return NotFound();

        if (string.IsNullOrWhiteSpace(req.UserName) || req.UserName.Length > 30)
            return BadRequest("UserName is required and must be <= 30 chars.");
        if (string.IsNullOrWhiteSpace(req.Email) || req.Email.Length > 100)
            return BadRequest("Email is required and must be <= 100 chars.");
        var role = (req.Role ?? user.Role).Trim();
        if (role.Length == 0 || role.Length > 20) return BadRequest("Role must be <= 20 chars.");

        var conflict = await _db.Users.AnyAsync(u =>
            u.UserId != id && (u.UserName == req.UserName || u.Email == req.Email));

        if (conflict) return Conflict("Username or email already exists.");

        user.UserName = req.UserName.Trim();
        user.Email = req.Email.Trim();
        user.Role = role;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser([FromRoute] int id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == id);
        if (user is null) return NotFound();

        var userUpgrades = await _db.UserUpgrades.Where(x => x.UserId == id).ToListAsync();
        _db.UserUpgrades.RemoveRange(userUpgrades);

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}