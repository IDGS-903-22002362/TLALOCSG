using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TLALOCSG.Models;

public partial class Review
{
    [Key]
    public int ReviewId { get; set; }

    public int ProductId { get; set; }

    [StringLength(450)]
    public string CustomerId { get; set; } = null!;

    public byte Rating { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsApproved { get; set; }

    [ForeignKey("CustomerId")]
    [InverseProperty("Reviews")]
    public virtual Customer Customer { get; set; } = null!;

    [ForeignKey("ProductId")]
    [InverseProperty("Reviews")]
    public virtual Product Product { get; set; } = null!;
}
