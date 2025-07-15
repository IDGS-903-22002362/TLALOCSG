using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TLALOCSG.Models;

[Index("SKU", Name = "UQ__Products__CA1ECF0D7FB1900E", IsUnique = true)]
public partial class Product
{
    [Key]
    public int ProductId { get; set; }

    [StringLength(50)]
    public string SKU { get; set; } = null!;

    [StringLength(200)]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal BasePrice { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [InverseProperty("Product")]
    public virtual ICollection<OrderLine> OrderLines { get; set; } = new List<OrderLine>();

    [InverseProperty("Product")]
    public virtual ICollection<ProductBOM> ProductBOMs { get; set; } = new List<ProductBOM>();

    [InverseProperty("Product")]
    public virtual ICollection<QuoteLine> QuoteLines { get; set; } = new List<QuoteLine>();

    [InverseProperty("Product")]
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
