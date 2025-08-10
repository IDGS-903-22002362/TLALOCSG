using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using TLALOCSG.Models;

namespace TLALOCSG.Data;

public partial class IoTIrrigationDbContext
{
    // DbSet nuevos
    public virtual DbSet<AccessRequest> AccessRequests { get; set; } = null!;
    public virtual DbSet<TicketMessage> TicketMessages { get; set; } = null!;
    public virtual DbSet<InstallTier> InstallTiers { get; set; } = null!;
    public virtual DbSet<StateRate> StateRates { get; set; } = null!;

    // Implementación del método parcial declarado en el archivo original
    partial void OnModelCreatingPartial(ModelBuilder b)
    {
        b.Entity<InstallTier>(e =>
        {
            e.ToTable("InstallTiers");
            e.HasKey(x => x.Id);
            e.Property(x => x.BaseCost).HasColumnType("decimal(18,2)");
            e.HasIndex(x => new { x.MinQty, x.MaxQty }).HasDatabaseName("IX_InstallTiers_Range");

            // seed ejemplo
            e.HasData(
                new InstallTier { Id = 1, MinQty = 1, MaxQty = 5, BaseCost = 2000m },
                new InstallTier { Id = 2, MinQty = 6, MaxQty = 15, BaseCost = 5500m },
                new InstallTier { Id = 3, MinQty = 16, MaxQty = null, BaseCost = 9000m }
            );
        });

        // ── StateRates ────────────────────────────────────────────────
        b.Entity<StateRate>(e =>
        {
            e.ToTable("StateRates");
            e.HasKey(x => x.StateCode);
            e.Property(x => x.StateCode).HasMaxLength(10);
            e.Property(x => x.StateName).HasMaxLength(80);
            e.Property(x => x.ShipPerKm).HasColumnType("decimal(18,2)");
            e.Property(x => x.TransportPerKm).HasColumnType("decimal(18,2)");

            e.HasData(
                // GRATIS en Guanajuato
                new StateRate { StateCode = "GTO", StateName = "Guanajuato", DistanceKm = 0, ShipPerKm = 0m, TransportPerKm = 0m },

                // Centro-Bajío
                new StateRate { StateCode = "QRO", StateName = "Querétaro", DistanceKm = 130, ShipPerKm = 6m, TransportPerKm = 10m },
                new StateRate { StateCode = "AGS", StateName = "Aguascalientes", DistanceKm = 180, ShipPerKm = 6m, TransportPerKm = 10m },
                new StateRate { StateCode = "SLP", StateName = "San Luis Potosí", DistanceKm = 220, ShipPerKm = 6m, TransportPerKm = 10m },
                new StateRate { StateCode = "MIC", StateName = "Michoacán", DistanceKm = 200, ShipPerKm = 6m, TransportPerKm = 10m },
                new StateRate { StateCode = "ZAC", StateName = "Zacatecas", DistanceKm = 350, ShipPerKm = 6m, TransportPerKm = 10m },
                new StateRate { StateCode = "COL", StateName = "Colima", DistanceKm = 470, ShipPerKm = 6m, TransportPerKm = 10m },
                new StateRate { StateCode = "JAL", StateName = "Jalisco", DistanceKm = 280, ShipPerKm = 6m, TransportPerKm = 10m },

                // Centro / Altiplano
                new StateRate { StateCode = "CDMX", StateName = "Ciudad de México", DistanceKm = 330, ShipPerKm = 6m, TransportPerKm = 10m },
                new StateRate { StateCode = "MEX", StateName = "Estado de México", DistanceKm = 280, ShipPerKm = 6m, TransportPerKm = 10m },
                new StateRate { StateCode = "HGO", StateName = "Hidalgo", DistanceKm = 400, ShipPerKm = 6m, TransportPerKm = 10m },
                new StateRate { StateCode = "MOR", StateName = "Morelos", DistanceKm = 420, ShipPerKm = 6m, TransportPerKm = 10m },
                new StateRate { StateCode = "PUE", StateName = "Puebla", DistanceKm = 520, ShipPerKm = 6m, TransportPerKm = 10m },
                new StateRate { StateCode = "TLA", StateName = "Tlaxcala", DistanceKm = 520, ShipPerKm = 6m, TransportPerKm = 10m },

                // Occidente / Pacífico
                new StateRate { StateCode = "NAY", StateName = "Nayarit", DistanceKm = 460, ShipPerKm = 6m, TransportPerKm = 10m },
                new StateRate { StateCode = "SIN", StateName = "Sinaloa", DistanceKm = 820, ShipPerKm = 6m, TransportPerKm = 10m },
                new StateRate { StateCode = "SON", StateName = "Sonora", DistanceKm = 1200, ShipPerKm = 6m, TransportPerKm = 10m },
                new StateRate { StateCode = "DUR", StateName = "Durango", DistanceKm = 600, ShipPerKm = 6m, TransportPerKm = 10m },

                // Norte
                new StateRate { StateCode = "NLE", StateName = "Nuevo León", DistanceKm = 700, ShipPerKm = 6m, TransportPerKm = 10m },
                new StateRate { StateCode = "COA", StateName = "Coahuila", DistanceKm = 800, ShipPerKm = 6m, TransportPerKm = 10m },
                new StateRate { StateCode = "CHH", StateName = "Chihuahua", DistanceKm = 1160, ShipPerKm = 6m, TransportPerKm = 10m },
                new StateRate { StateCode = "TAM", StateName = "Tamaulipas", DistanceKm = 800, ShipPerKm = 6m, TransportPerKm = 10m },

                // Sur / Sureste
                new StateRate { StateCode = "VER", StateName = "Veracruz", DistanceKm = 650, ShipPerKm = 6m, TransportPerKm = 10m },
                new StateRate { StateCode = "OAX", StateName = "Oaxaca", DistanceKm = 740, ShipPerKm = 6m, TransportPerKm = 10m },
                new StateRate { StateCode = "GRO", StateName = "Guerrero", DistanceKm = 650, ShipPerKm = 6m, TransportPerKm = 10m },
                new StateRate { StateCode = "TAB", StateName = "Tabasco", DistanceKm = 1050, ShipPerKm = 6m, TransportPerKm = 10m },
                new StateRate { StateCode = "CAM", StateName = "Campeche", DistanceKm = 1350, ShipPerKm = 6m, TransportPerKm = 10m },
                new StateRate { StateCode = "YUC", StateName = "Yucatán", DistanceKm = 1550, ShipPerKm = 6m, TransportPerKm = 10m },
                new StateRate { StateCode = "ROO", StateName = "Quintana Roo", DistanceKm = 1800, ShipPerKm = 6m, TransportPerKm = 10m },
                new StateRate { StateCode = "CHP", StateName = "Chiapas", DistanceKm = 1100, ShipPerKm = 6m, TransportPerKm = 10m },

                // Península de Baja
                new StateRate { StateCode = "BCN", StateName = "Baja California", DistanceKm = 2300, ShipPerKm = 6m, TransportPerKm = 10m },
                new StateRate { StateCode = "BCS", StateName = "Baja California Sur", DistanceKm = 1600, ShipPerKm = 6m, TransportPerKm = 10m }
            );
        });

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
        b.Entity<Quote>(e =>
        {
            e.Property(x => x.Fulfillment).HasMaxLength(20);
            e.Property(x => x.StateCode).HasMaxLength(10);

            e.Property(x => x.TotalProducts).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
            e.Property(x => x.InstallBase).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
            e.Property(x => x.TransportCost).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
            e.Property(x => x.ShippingCost).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
            e.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        });

    }
}
