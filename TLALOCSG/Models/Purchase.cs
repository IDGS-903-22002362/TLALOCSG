using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TLALOCSG.Models;

public partial class Purchase
{
    [Key]
    public int PurchaseId { get; set; }

    public int SupplierId { get; set; }

    public DateTime PurchaseDate { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = null!;

    [Column(TypeName = "decimal(18, 4)")]
    public decimal TotalAmount { get; set; }

    [StringLength(450)]
    public string CreatedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    [ForeignKey("CreatedBy")]
    [InverseProperty("Purchases")]
    public virtual Admin CreatedByNavigation { get; set; } = null!;

    [InverseProperty("Purchase")]
    public virtual ICollection<PurchaseLine> PurchaseLines { get; set; } = new List<PurchaseLine>();

    [ForeignKey("SupplierId")]
    [InverseProperty("Purchases")]
    public virtual Supplier Supplier { get; set; } = null!;
}
