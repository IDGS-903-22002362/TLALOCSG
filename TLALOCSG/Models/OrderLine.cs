using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TLALOCSG.Models;

public partial class OrderLine
{
    [Key]
    public int OrderLineId { get; set; }

    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(29, 4)")]
    public decimal? LineTotal { get; set; }

    [ForeignKey("OrderId")]
    [InverseProperty("OrderLines")]
    public virtual Order Order { get; set; } = null!;

    [ForeignKey("ProductId")]
    [InverseProperty("OrderLines")]
    public virtual Product Product { get; set; } = null!;
}
