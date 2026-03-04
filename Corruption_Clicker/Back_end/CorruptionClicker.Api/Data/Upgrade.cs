using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CorruptionClicker.Api.Data;

[Table("upgrades")] 
public class Upgrade
{
    [Key]
    [Column("upgrade_id")]
    public int UpgradeId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("image_url")]
    public string ImageUrl { get; set; } = string.Empty;

    [Column("base_cost")]
    public int BaseCost { get; set; }

    [Column("cps_bonus")]
    public double CpsBonus { get; set; }

    [Column("cpc_bonus")]
    public int CpcBonus { get; set; }

    [Column("max_quantity")]
    public int MaxQuantity { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }
}