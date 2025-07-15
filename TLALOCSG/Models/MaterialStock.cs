using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TLALOCSG.Models;

[PrimaryKey("MaterialId", "Location")]
[Index("QuantityOnHand", Name = "IX_MaterialStocks_Qty")]
public partial class MaterialStock
{
    [Key]
    public int MaterialId { get; set; }

    [Key]
    [StringLength(50)]
    public string Location { get; set; } = null!;

    [Column(TypeName = "decimal(18, 4)")]
    public decimal QuantityOnHand { get; set; }

    [ForeignKey("MaterialId")]
    [InverseProperty("MaterialStocks")]
    public virtual Material Material { get; set; } = null!;
}
