using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TLALOCSG.Models;

[Index("SKU", Name = "UQ__Material__CA1ECF0D5CB292B8", IsUnique = true)]
public partial class Material
{
    [Key]
    public int MaterialId { get; set; }

    [StringLength(50)]
    public string SKU { get; set; } = null!;

    [StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(100)]
    public string? Category { get; set; }

    [StringLength(20)]
    public string UnitOfMeasure { get; set; } = null!;

    [Column(TypeName = "decimal(18, 4)")]
    public decimal StandardCost { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Material")]
    public virtual ICollection<MaterialMovement> MaterialMovements { get; set; } = new List<MaterialMovement>();

    [InverseProperty("Material")]
    public virtual ICollection<MaterialStock> MaterialStocks { get; set; } = new List<MaterialStock>();

    [InverseProperty("Material")]
    public virtual ICollection<ProductBOM> ProductBOMs { get; set; } = new List<ProductBOM>();

    [InverseProperty("Material")]
    public virtual ICollection<PurchaseLine> PurchaseLines { get; set; } = new List<PurchaseLine>();
}
