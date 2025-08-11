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

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<AdminDashboardDto>> GetAdmin(
    [FromQuery] DateTime? from,
    [FromQuery] DateTime? to,
    [FromQuery] int top = 5,
    [FromQuery] int lowStockThreshold = 5)
    {
        var (start, end) = NormalizeRange(from, to);

        // Órdenes filtradas del rango (excluye Cancelled)
        var ordersQ = _ctx.Orders.AsNoTracking()
            .Where(o => o.OrderDate >= start &&
                        o.OrderDate <= end &&
                        o.Status != "Cancelled");

        // Serie de ventas por día → proyecta anónimo y mapea a DTO después
        var salesRaw = await ordersQ
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new { Date = g.Key, Value = g.Sum(x => x.TotalAmount) })
            .OrderBy(x => x.Date)
            .ToListAsync();

        var salesByDay = salesRaw.Select(x => new SeriesPointDto(x.Date, x.Value)).ToList();
        var salesTotal = salesRaw.Sum(x => x.Value);
        var ordersCount = await ordersQ.CountAsync();

        // KPIs extra
        var pendingQuotes = await _ctx.Quotes.CountAsync(q => q.Status == "Draft");
        var openTickets = await _ctx.Tickets.CountAsync(t => t.Status != "Closed");

        // Top productos: parte de Orders filtradas → SelectMany a OrderLines → GroupBy
        var topProductsRaw = await _ctx.Orders.AsNoTracking()
            .Where(o => o.OrderDate >= start &&
                        o.OrderDate <= end &&
                        o.Status != "Cancelled")
            .SelectMany(o => o.OrderLines.Select(l => new
            {
                l.ProductId,
                ProductName = l.Product != null ? l.Product.Name : "",
                Qty = l.Quantity,
                LineTotal = l.Quantity * l.UnitPrice
            }))
            .GroupBy(x => new { x.ProductId, x.ProductName })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.ProductName,
                Units = g.Sum(x => x.Qty),
                Total = g.Sum(x => x.LineTotal)
            })
            .OrderByDescending(x => x.Total)
            .ThenByDescending(x => x.Units)
            .Take(top)
            .ToListAsync();

        var topProducts = topProductsRaw
            .Select(x => new TopProductDto(x.ProductId, x.ProductName, x.Units, x.Total))
            .ToList();

        // Bajo stock: suma por MaterialId y luego join a Materials; mapea tras ToListAsync
        var lowStockRaw = await _ctx.MaterialStocks.AsNoTracking()
            .GroupBy(s => s.MaterialId)
            .Select(g => new { MaterialId = g.Key, OnHand = g.Sum(x => x.QuantityOnHand) })
            .Where(x => x.OnHand < lowStockThreshold)
            .Join(_ctx.Materials.AsNoTracking(),
                  s => s.MaterialId, m => m.MaterialId,
                  (s, m) => new { m.MaterialId, m.Name, s.OnHand })
            .OrderBy(x => x.OnHand)
            .ToListAsync();

        var lowStock = lowStockRaw
            .Select(x => new LowStockDto(x.MaterialId, x.Name, x.OnHand))
            .ToList();

        return new AdminDashboardDto
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
