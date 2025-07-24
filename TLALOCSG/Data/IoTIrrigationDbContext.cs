using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using TLALOCSG.Models;

namespace TLALOCSG.Data;

public partial class IoTIrrigationDbContext : IdentityDbContext<ApplicationUser>
{
    public IoTIrrigationDbContext()
    {
    }

    public IoTIrrigationDbContext(DbContextOptions<IoTIrrigationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<FAQ> FAQs { get; set; }

    public virtual DbSet<Material> Materials { get; set; }

    public virtual DbSet<MaterialMovement> MaterialMovements { get; set; }

    public virtual DbSet<MaterialStock> MaterialStocks { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderLine> OrderLines { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductBOM> ProductBOMs { get; set; }

    public virtual DbSet<Purchase> Purchases { get; set; }

    public virtual DbSet<PurchaseLine> PurchaseLines { get; set; }

    public virtual DbSet<Quote> Quotes { get; set; }

    public virtual DbSet<QuoteLine> QuoteLines { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<Ticket> Tickets { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-NGUHHUR;Database=IoTIrrigationDB;User Id=sa;Password=Uucy291o;TrustServerCertificate=True;MultipleActiveResultSets=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.AdminId).HasName("PK__Admins__719FE488130953B8");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__Customer__A4AE64D80DEFF95F");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<FAQ>(entity =>
        {
            entity.HasKey(e => e.FaqId).HasName("PK__FAQs__9C741C4327B6B992");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<Material>(entity =>
        {
            entity.HasKey(e => e.MaterialId).HasName("PK__Material__C50610F724A82E50");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<MaterialMovement>(entity =>
        {
            entity.HasKey(e => e.MovementId).HasName("PK__Material__D1822446194917B1");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.MovementDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.MovementType).IsFixedLength();

            entity.HasOne(d => d.Material).WithMany(p => p.MaterialMovements)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Mov_Material");
        });

        modelBuilder.Entity<MaterialStock>(entity =>
        {
            entity.HasKey(e => new { e.MaterialId, e.Location }).HasName("PK__Material__CB53C346F1C742F4");

            entity.Property(e => e.Location).HasDefaultValue("MAIN");

            entity.HasOne(d => d.Material).WithMany(p => p.MaterialStocks).HasConstraintName("FK_Stocks_Material");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BCF11E7A450");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.OrderDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Status).HasDefaultValue("Pending");

            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Customer");

            entity.HasOne(d => d.Quote).WithMany(p => p.Orders).HasConstraintName("FK_Orders_Quote");
        });

        modelBuilder.Entity<OrderLine>(entity =>
        {
            entity.HasKey(e => e.OrderLineId).HasName("PK__OrderLin__29068A107C93D82E");

            entity.Property(e => e.LineTotal).HasComputedColumnSql("([Quantity]*[UnitPrice])", true);

            entity.HasOne(d => d.Order).WithMany(p => p.OrderLines).HasConstraintName("FK_OL_Order");

            entity.HasOne(d => d.Product).WithMany(p => p.OrderLines)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OL_Product");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A38AFCDE9C5");

            entity.Property(e => e.PaymentDate).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments).HasConstraintName("FK_Payments_Order");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Products__B40CC6CD821C131D");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<ProductBOM>(entity =>
        {
            entity.HasKey(e => new { e.ProductId, e.MaterialId }).HasName("PK__ProductB__D85CA7C2FDD4AB41");

            entity.HasOne(d => d.Material).WithMany(p => p.ProductBOMs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BOM_Material");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductBOMs).HasConstraintName("FK_BOM_Product");
        });

        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.HasKey(e => e.PurchaseId).HasName("PK__Purchase__6B0A6BBE7BA1A4E2");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.PurchaseDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Status).HasDefaultValue("Pending");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Purchases)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Purchases_Admin");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Purchases)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Purchases_Supplier");
        });

        modelBuilder.Entity<PurchaseLine>(entity =>
        {
            entity.HasKey(e => e.PurchaseLineId).HasName("PK__Purchase__8BC954DECB5CCC81");

            entity.Property(e => e.LineTotal).HasComputedColumnSql("([Quantity]*[UnitCost])", true);

            entity.HasOne(d => d.Material).WithMany(p => p.PurchaseLines)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PL_Material");

            entity.HasOne(d => d.Purchase).WithMany(p => p.PurchaseLines).HasConstraintName("FK_PL_Purchase");
        });

        modelBuilder.Entity<Quote>(entity =>
        {
            entity.HasKey(e => e.QuoteId).HasName("PK__Quotes__AF9688C1F02B6501");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.QuoteDate).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Status).HasDefaultValue("Draft");

            entity.HasOne(d => d.Customer).WithMany(p => p.Quotes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Quotes_Customer");
        });

        modelBuilder.Entity<QuoteLine>(entity =>
        {
            entity.HasKey(e => e.QuoteLineId).HasName("PK__QuoteLin__89C6C90B373D28AF");

            entity.Property(e => e.LineTotal).HasComputedColumnSql("([Quantity]*[UnitPrice])", true);

            entity.HasOne(d => d.Product).WithMany(p => p.QuoteLines)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QL_Product");

            entity.HasOne(d => d.Quote).WithMany(p => p.QuoteLines).HasConstraintName("FK_QL_Quote");
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__Reviews__74BC79CE232BA148");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Customer).WithMany(p => p.Reviews)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reviews_Customer");

            entity.HasOne(d => d.Product).WithMany(p => p.Reviews)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reviews_Product");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.SupplierId).HasName("PK__Supplier__4BE666B467D34C7D");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysutcdatetime())");
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.TicketId).HasName("PK__Tickets__712CC607037D004A");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Status).HasDefaultValue("Open");

            entity.HasOne(d => d.Customer).WithMany(p => p.Tickets)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tickets_Customer");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
