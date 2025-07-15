using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TLALOCSG.Models;

[Index("Status", "OrderDate", Name = "IX_Orders_Status_Date", IsDescending = new[] { false, true })]
public partial class Order
{
    [Key]
    public int OrderId { get; set; }

    public int? QuoteId { get; set; }

    [StringLength(450)]
    public string CustomerId { get; set; } = null!;

    public DateTime OrderDate { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = null!;

    [Column(TypeName = "decimal(18, 4)")]
    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("CustomerId")]
    [InverseProperty("Orders")]
    public virtual Customer Customer { get; set; } = null!;

    [InverseProperty("Order")]
    public virtual ICollection<OrderLine> OrderLines { get; set; } = new List<OrderLine>();

    [InverseProperty("Order")]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    [ForeignKey("QuoteId")]
    [InverseProperty("Orders")]
    public virtual Quote? Quote { get; set; }
}
