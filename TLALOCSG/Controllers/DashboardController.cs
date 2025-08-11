using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TLALOCSG.Data;
using TLALOCSG.DTOs.Dashboard;

namespace TLALOCSG.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IoTIrrigationDbContext _ctx;
    private readonly IMemoryCache _cache;

    public DashboardController(IoTIrrigationDbContext ctx, IMemoryCache cache)
    {
        _ctx = ctx;
        _cache = cache;
    }

    // GET: /api/dashboard/admin?from=2025-01-01&to=2025-01-31&top=5&lowStockThreshold=5
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AdminDashboardDto>> GetAdmin(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int top = 5,
        [FromQuery] int lowStockThreshold = 5)
    {
        var (start, end) = NormalizeRange(from, to);

        // cache 30s para evitar golpear BD en refrescos
        var cacheKey = $"dash_admin_{start:yyyyMMdd}_{end:yyyyMMdd}_top{top}_ls{lowStockThreshold}";
        if (_cache.TryGetValue(cacheKey, out AdminDashboardDto? cached) && cached is not null)
            return cached;

        // Órdenes del rango (excluye Cancelled)
        var ordersQ = _ctx.Orders.AsNoTracking()
            .Where(o => o.OrderDate >= start && o.OrderDate <= end && o.Status != "Cancelled");

        var salesRaw = await _ctx.Orders.AsNoTracking()
    .Where(o => o.OrderDate >= start && o.OrderDate <= end && o.Status != "Cancelled")
    .GroupBy(o => o.OrderDate.Date)
    .Select(g => new { Date = g.Key, Value = g.Sum(x => x.TotalAmount) })
    .OrderBy(x => x.Date)
    .ToListAsync();

        var salesByDay = salesRaw.Select(x => new SeriesPointDto(x.Date, x.Value)).ToList();


        var salesTotal = salesByDay.Sum(x => x.Value);
        var ordersCount = await ordersQ.CountAsync();

        // KPIs que no dependen del rango
        var pendingQuotes = await _ctx.Quotes.CountAsync(q => q.Status == "Draft");
        var openTickets = await _ctx.Tickets.CountAsync(t => t.Status != "Closed");

        // Top productos por ventas en $ y unidades en el rango
        var topProducts = await _ctx.OrderLines
    .AsNoTracking()
    .Where(l => l.Order.OrderDate >= start &&
                l.Order.OrderDate <= end &&
                l.Order.Status != "Cancelled")
    // Proyección previa (con alias) para que EF lo traduzca bien
    .Select(l => new
    {
        l.ProductId,
        ProductName = l.Product != null ? l.Product.Name : "",   // alias y sin '!'
        Qty = l.Quantity,
        LineTotal = l.Quantity * l.UnitPrice
    })
    .GroupBy(x => new { x.ProductId, x.ProductName })
    .Select(g => new TopProductDto(
        g.Key.ProductId,
        g.Key.ProductName,
        g.Sum(x => x.Qty),
        g.Sum(x => x.LineTotal)
    ))
    .OrderByDescending(x => x.Total)
    .ThenByDescending(x => x.Units)
    .Take(top)
    .ToListAsync();

        // Bajo stock (sumatorio por MaterialId)
        var lowStock = await _ctx.MaterialStocks
            .AsNoTracking()
            .GroupBy(s => s.MaterialId)
            .Select(g => new { g.Key, Qty = g.Sum(x => x.QuantityOnHand) })
            .Where(x => x.Qty < lowStockThreshold)
            .Join(_ctx.Materials.AsNoTracking(),
                s => s.Key, m => m.MaterialId,
                (s, m) => new LowStockDto(m.MaterialId, m.Name, s.Qty))
            .OrderBy(x => x.OnHand)
            .ToListAsync();

        var dto = new AdminDashboardDto
        {
            Kpis = new KpiDto(
                Sales: salesTotal,
                Orders: ordersCount,
                PendingQuotes: pendingQuotes,
                OpenTickets: openTickets,
                LowStock: lowStock.Count
            ),
            SalesByDay = salesByDay,
            TopProducts = topProducts,
            LowStock = lowStock
        };

        _cache.Set(cacheKey, dto, TimeSpan.FromSeconds(30));
        return dto;
    }

    // GET: /api/dashboard/me?from=&to=
    // GET /api/dashboard/me
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ClientDashboardDto>> GetMe(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var (start, end) = NormalizeRange(from, to);
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var myOrdersQ = _ctx.Orders.AsNoTracking()
            .Where(o => o.CustomerId == userId
                     && o.OrderDate >= start
                     && o.OrderDate <= end
                     && o.Status != "Cancelled");

        // ⬇️ Proyección anónima -> ToListAsync -> map a DTO
        var mySeriesRaw = await myOrdersQ
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Value = g.Sum(x => x.TotalAmount)
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        var mySeries = mySeriesRaw
            .Select(x => new SeriesPointDto(x.Date, x.Value))
            .ToList();

        // KPIs cliente (igual que antes)
        var myDraftQuotes = await _ctx.Quotes.CountAsync(q => q.CustomerId == userId && q.Status == "Draft");
        var myApprovedQuotes = await _ctx.Quotes.CountAsync(q => q.CustomerId == userId && q.Status == "Approved");
        var myOpenTickets = await _ctx.Tickets.CountAsync(t => t.CustomerId == userId && t.Status != "Closed");

        var last30 = DateTime.UtcNow.Date.AddDays(-30);
        var myOrdersTotalLast30d = await _ctx.Orders.AsNoTracking()
            .Where(o => o.CustomerId == userId && o.Status != "Cancelled" && o.OrderDate >= last30)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

        var dto = new ClientDashboardDto
        {
            Kpis = new ClientKpis(
                MyDraftQuotes: myDraftQuotes,
                MyApprovedQuotes: myApprovedQuotes,
                MyOpenTickets: myOpenTickets,
                MyOrdersTotalLast30d: myOrdersTotalLast30d
            ),
            MyOrdersByDay = mySeries
        };

        return dto;
    }


    private static (DateTime start, DateTime end) NormalizeRange(DateTime? from, DateTime? to)
    {
        var end = (to ?? DateTime.UtcNow.Date).Date.AddDays(1).AddTicks(-1); // fin del día
        var start = (from ?? end.Date.AddDays(-29)).Date;                    // 30 días por defecto
        if (start > end) (start, end) = (end, start);
        return (start, end);
    }
}
