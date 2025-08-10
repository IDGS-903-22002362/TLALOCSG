using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TLALOCSG.Models;

public class StateRate
{
    [Key, StringLength(10)]
    public string StateCode { get; set; } = null!;   // ej. "GTO"

    [StringLength(80)]
    public string StateName { get; set; } = null!;   // "Guanajuato"

    public int DistanceKm { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ShipPerKm { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TransportPerKm { get; set; }
}
