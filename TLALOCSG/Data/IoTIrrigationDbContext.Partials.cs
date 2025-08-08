using Microsoft.EntityFrameworkCore;
using TLALOCSG.Models;

namespace TLALOCSG.Data;

public partial class IoTIrrigationDbContext
{
    // DbSet nuevos
    public virtual DbSet<AccessRequest> AccessRequests { get; set; } = null!;
    public virtual DbSet<TicketMessage> TicketMessages { get; set; } = null!;

    // Implementación del método parcial declarado en el archivo original
    partial void OnModelCreatingPartial(ModelBuilder b)
    {
        // AccessRequests
        b.Entity<AccessRequest>(e =>
        {
            e.ToTable("AccessRequests");
            e.Property(p => p.Email).HasMaxLength(256).IsRequired();
            e.Property(p => p.FullName).HasMaxLength(150);
            e.Property(p => p.Status).HasMaxLength(20).HasDefaultValue("Pending");
            e.Property(p => p.CreatedAt).HasColumnType("datetime2").HasDefaultValueSql("GETUTCDATE()");
            e.Property(p => p.ProcessedAt).HasColumnType("datetime2");
            e.Property(p => p.ProcessedBy).HasMaxLength(450);
            e.Property(p => p.Note).HasMaxLength(500);

            e.HasIndex(p => p.Email).HasDatabaseName("IX_AccessRequests_Email");
            e.HasIndex(p => new { p.Email, p.Status }).HasDatabaseName("IX_AccessRequests_Email_Status");
            // Índice único opcional de pendientes:
            // e.HasIndex(p => p.Email).IsUnique().HasFilter("[Status] = 'Pending'")
            //  .HasDatabaseName("UX_AccessRequests_Email_Pending");
        });

        // TicketMessages
        b.Entity<TicketMessage>(e =>
        {
            e.ToTable("TicketMessages");
            e.HasKey(p => p.MessageId);
            e.Property(p => p.SenderId).HasMaxLength(450).IsRequired();
            e.Property(p => p.Body).IsRequired();
            e.Property(p => p.CreatedAt).HasColumnType("datetime2")
                .HasDefaultValueSql("GETUTCDATE()");

            e.HasIndex(p => p.TicketId).HasDatabaseName("IX_TicketMessages_TicketId");

            e.HasOne(p => p.Ticket)
             .WithMany() // si tu entidad Ticket no tiene ICollection<TicketMessage>
             .HasForeignKey(p => p.TicketId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
