using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TLALOCSG.Models;

public partial class Quote
{
    [Key]
    public int QuoteId { get; set; }

    [StringLength(450)]
    public string CustomerId { get; set; } = null!;

    public DateTime QuoteDate { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = null!;

    [Column(TypeName = "decimal(18, 4)")]
    public decimal TotalAmount { get; set; }

    public DateTime? ValidUntil { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("CustomerId")]
    [InverseProperty("Quotes")]
    public virtual Customer Customer { get; set; } = null!;

    [InverseProperty("Quote")]
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    [InverseProperty("Quote")]
    public virtual ICollection<QuoteLine> QuoteLines { get; set; } = new List<QuoteLine>();
}
