using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CorruptionClicker.Api.Data;

[Table("users")] 
public class User
{
    [Key]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("role")]
    public string Role { get; set; } = "User";

    [Required]
    [MaxLength(30)]
    [Column("user_name")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("cash_balance")]
    public long CashBalance { get; set; }

    [Column("cash_per_click")]
    public int CashPerClick { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

}