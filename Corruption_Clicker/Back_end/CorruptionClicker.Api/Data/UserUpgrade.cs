using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;

namespace CorruptionClicker.Api.Data;

[Table("user_upgrades")] 
public class UserUpgrade
{
    [Key]
    [Column("user_upgrade_id")]
    public int UserUpgradeId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; } = 0;

    [Column("upgrade_id")]
    public int UpgradeId { get; set; } = 0;

    [Column("quantity")]
    public int Quantity { get; set; } = 0;

    [Column("total_spent")]
    public long TotalSpent { get; set; } = 0;

    [Column("is_equipped")]
    public bool IsEquipped { get; set; } = true;

    [Column("last_purchased_at")]
    public DateTimeOffset? LastPurchasedAt { get; set; }

}