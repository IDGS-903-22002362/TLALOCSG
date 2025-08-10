using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TLALOCSG.Models;

public partial class Quote
{
    // DevicesOnly | Shipping | Installation
    [StringLength(20)]
    public string? Fulfillment { get; set; }

    // Clave de estado (ej. "GTO"). Nullable si DevicesOnly
    [StringLength(10)]
    public string? StateCode { get; set; }

    // Desgloses (todos currency)
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalProducts { get; set; }   // suma de líneas

    [Column(TypeName = "decimal(18,2)")]
    public decimal InstallBase { get; set; }     // base de instalación (por tier)

    [Column(TypeName = "decimal(18,2)")]
    public decimal TransportCost { get; set; }   // transporte para instalación

    [Column(TypeName = "decimal(18,2)")]
    public decimal ShippingCost { get; set; }    // costo de envío
    public DateTime? UpdatedAt { get; set; }

}
