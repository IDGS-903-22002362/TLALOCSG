using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TLALOCSG.Models;

public partial class PurchaseLine
{
    [Key]
    public int PurchaseLineId { get; set; }

    public int PurchaseId { get; set; }

    public int MaterialId { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal UnitCost { get; set; }

    [Column(TypeName = "decimal(37, 8)")]
    public decimal? LineTotal { get; set; }

    [ForeignKey("MaterialId")]
    [InverseProperty("PurchaseLines")]
    public virtual Material Material { get; set; } = null!;

    [ForeignKey("PurchaseId")]
    [InverseProperty("PurchaseLines")]
    public virtual Purchase Purchase { get; set; } = null!;
}
