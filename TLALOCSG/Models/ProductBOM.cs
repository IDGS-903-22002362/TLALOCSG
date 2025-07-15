using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TLALOCSG.Models;

[PrimaryKey("ProductId", "MaterialId")]
[Table("ProductBOM")]
public partial class ProductBOM
{
    [Key]
    public int ProductId { get; set; }

    [Key]
    public int MaterialId { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal Quantity { get; set; }

    [ForeignKey("MaterialId")]
    [InverseProperty("ProductBOMs")]
    public virtual Material Material { get; set; } = null!;

    [ForeignKey("ProductId")]
    [InverseProperty("ProductBOMs")]
    public virtual Product Product { get; set; } = null!;
}
