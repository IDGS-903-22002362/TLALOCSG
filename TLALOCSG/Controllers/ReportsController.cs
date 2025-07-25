using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using TLALOCSG.Data;
using TLALOCSG.DTOs.Reports;

namespace TLALOCSG.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly IoTIrrigationDbContext _ctx;

    public ReportsController(IoTIrrigationDbContext ctx) => _ctx = ctx;

    //VENTAS 
    [HttpGet("sales")]
    public async Task<IActionResult> GetSales([FromQuery] DateTime from, [FromQuery] DateTime to,
                                              [FromQuery] string? format = "json")
    {
        var data = await _ctx.Orders
            .Where(o => o.OrderDate >= from && o.OrderDate <= to && o.Status != "Cancelled")
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new SalesReportLineDto(
                g.Key,
                g.Count(),
                g.SelectMany(o => o.OrderLines).Sum(l => l.Quantity),
                g.Sum(o => o.TotalAmount)))
            .OrderBy(x => x.Date)
            .ToListAsync();

        return format?.ToLower() switch
        {
            "csv" => File(BuildSalesCsv(data), "text/csv",
                          $"sales_{from:yyyyMMdd}_{to:yyyyMMdd}.csv"),
            "pdf" => File(BuildSalesPdf(data), "application/pdf",
                          $"sales_{from:yyyyMMdd}_{to:yyyyMMdd}.pdf"),
            _ => Ok(data)
        };
    }

    //INVENTORY ROTATION
    [HttpGet("inventory-rotation")]
    public async Task<IEnumerable<RotationDto>> GetRotation([FromQuery] int days = 30)
    {
        var dateFrom = DateTime.UtcNow.AddDays(-days);

        // consumo promedio por material
        var outs = await _ctx.MaterialMovements
            .Where(m => m.MovementType == "O" && m.MovementDate >= dateFrom)
            .GroupBy(m => m.MaterialId)
            .Select(g => new { g.Key, AvgDailyOut = g.Sum(x => x.Quantity) / days })
            .ToDictionaryAsync(x => x.Key, x => x.AvgDailyOut);

        var stock = await _ctx.MaterialStocks
            .GroupBy(s => s.MaterialId)
            .Select(g => new { g.Key, Qty = g.Sum(x => x.QuantityOnHand) })
            .ToListAsync();

        var materials = await _ctx.Materials.ToDictionaryAsync(m => m.MaterialId, m => m.Name);

        return stock.Select(s =>
        {
            outs.TryGetValue(s.Key, out var avg);
            var daysSupply = avg > 0 ? Math.Round(s.Qty / avg, 1) : decimal.MaxValue;
            return new RotationDto(s.Key, materials[s.Key], s.Qty, avg, daysSupply);
        });
    }

    //COSTS & MARGINS 
    [HttpGet("costs-margins")]
    public async Task<IEnumerable<MarginDto>> GetCostsMargins([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var sales = _ctx.OrderLines
            .Include(l => l.Order)
            .Where(l => l.Order.Status != "Cancelled");

        if (from.HasValue) sales = sales.Where(l => l.Order.OrderDate >= from.Value);
        if (to.HasValue) sales = sales.Where(l => l.Order.OrderDate <= to.Value);

        var salesAgg = await sales
            .GroupBy(l => l.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                AvgPrice = g.Average(x => x.UnitPrice)
            }).ToDictionaryAsync(x => x.ProductId, x => x.AvgPrice);

        var products = await _ctx.Products
            .Select(p => new { p.ProductId, p.Name, p.BasePrice })
            .ToListAsync();

        return products.Select(p =>
        {
            salesAgg.TryGetValue(p.ProductId, out var avgSale);
            var margin = avgSale > 0 ? Math.Round((avgSale - p.BasePrice) / avgSale * 100, 2) : 0;
            return new MarginDto(p.ProductId, p.Name, p.BasePrice, avgSale, margin);
        });
    }

    //Helper CSV 
    private static byte[] BuildSalesCsv(IEnumerable<SalesReportLineDto> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Date,Orders,Units,Total");
        foreach (var r in rows)
            sb.AppendLine($"{r.Date:yyyy-MM-dd},{r.Orders},{r.Units},{r.Total.ToString(CultureInfo.InvariantCulture)}");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    //Helper PDF 
    private static byte[] BuildSalesPdf(IEnumerable<SalesReportLineDto> rows)
    {
        // Placeholder: conviértelo a texto fijo en bytes; en producción usa una librería PDF.
        var sb = new StringBuilder();
        sb.AppendLine("Sales Report");
        sb.AppendLine("-------------------------------------");
        foreach (var r in rows)
            sb.AppendLine($"{r.Date:yyyy-MM-dd}  Orders:{r.Orders}  Units:{r.Units}  Total:{r.Total:C}");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
