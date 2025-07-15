using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TLALOCSG.Models;

public partial class MaterialMovement
{
    [Key]
    public long MovementId { get; set; }

    public int MaterialId { get; set; }

    [StringLength(1)]
    [Unicode(false)]
    public string MovementType { get; set; } = null!;

    [Column(TypeName = "decimal(18, 4)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(18, 4)")]
    public decimal UnitCost { get; set; }

    public DateTime MovementDate { get; set; }

    [StringLength(100)]
    public string? Reference { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("MaterialId")]
    [InverseProperty("MaterialMovements")]
    public virtual Material Material { get; set; } = null!;
}
