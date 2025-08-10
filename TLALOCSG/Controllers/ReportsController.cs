using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TLALOCSG.Data;
using TLALOCSG.Services.Reporting;

namespace TLALOCSG.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly IoTIrrigationDbContext _ctx;
    public ReportsController(IoTIrrigationDbContext ctx) => _ctx = ctx;

    /*──────────────────────── Helpers comunes ────────────────────────*/
    private static (DateTime from, DateTime to) NormalizeRange(DateTime? from, DateTime? to)
    {
        var f = from ?? DateTime.UtcNow.Date.AddDays(-30);
        var t = to ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);
        if (f > t) (f, t) = (t, f);
        return (DateTime.SpecifyKind(f, DateTimeKind.Utc),
                DateTime.SpecifyKind(t, DateTimeKind.Utc));
    }

    private static string EscapeCsv(string s)
    {
        if (s is null) return "";
        var needs = s.Contains(',') || s.Contains('"') || s.Contains('\n');
        var v = s.Replace("\"", "\"\"");
        return needs ? $"\"{v}\"" : v;
    }

    private static FileContentResult Csv<T>(IEnumerable<T> rows, string[] headers, Func<T, string[]> selector, string name)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", headers));
        foreach (var r in rows)
            sb.AppendLine(string.Join(",", selector(r).Select(EscapeCsv)));
        return new FileContentResult(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv")
        { FileDownloadName = name };
    }

    /*──────────────────────── Ventas por día ─────────────────────────
      Filtros extra:
      - tzOffset (minutos) para agrupar por fecha local
      - customerId?  productId?
      - format = json | csv | pdf
    ─────────────────────────────────────────────────────────────────*/
    [HttpGet("sales")]
    public async Task<IActionResult> GetSales(
    [FromQuery] DateTime? from,
    [FromQuery] DateTime? to,
    [FromQuery] int? tzOffset,
    [FromQuery] string? customerId,
    [FromQuery] int? productId,
    [FromQuery] string? format = "json")
    {
        var (f, t) = NormalizeRange(from, to);

        // Base query (sin Include; EF traduce Sum sobre la nav sin Include)
        var q = _ctx.Orders.AsNoTracking()
            .Where(o => o.Status != "Cancelled" && o.OrderDate >= f && o.OrderDate <= t);

        if (!string.IsNullOrWhiteSpace(customerId))
            q = q.Where(o => o.CustomerId == customerId);

        if (productId.HasValue)
            q = q.Where(o => o.OrderLines.Any(l => l.ProductId == productId.Value));

        // 1) Proyecta lo mínimo por orden y materialízalo
        var raw = await q
            .Select(o => new
            {
                o.OrderDate,
                Orders = 1,
                Units = o.OrderLines.Sum(l => l.Quantity),
                Total = o.TotalAmount
            })
            .ToListAsync();

        // 2) Ajuste de zona y agrupación en memoria (soluciona el error)
        var data = raw
            .Select(x => new
            {
                LocalDate = tzOffset.HasValue ? x.OrderDate.AddMinutes(tzOffset.Value).Date : x.OrderDate.Date,
                x.Orders,
                x.Units,
                x.Total
            })
            .GroupBy(x => x.LocalDate)
            .Select(g => new SalesReportLineDto(
                g.Key,
                g.Sum(v => v.Orders),
                g.Sum(v => v.Units),
                Math.Round(g.Sum(v => v.Total), 2)))
            .OrderBy(r => r.Date)
            .ToList();

        var header = new
        {
            From = f,
            To = t,
            Orders = data.Sum(x => x.Orders),
            Units = data.Sum(x => x.Units),
            Total = data.Sum(x => x.Total)
        };

        switch ((format ?? "json").ToLowerInvariant())
        {
            case "csv":
                return Csv(
                    data,
                    new[] { "Date", "Orders", "Units", "Total" },
                    r => new[] {
                    r.Date.ToString("yyyy-MM-dd"),
                    r.Orders.ToString(CultureInfo.InvariantCulture),
                    r.Units.ToString(CultureInfo.InvariantCulture),
                    r.Total.ToString(CultureInfo.InvariantCulture)
                    },
                    $"sales_{f:yyyyMMdd}_{t:yyyyMMdd}.csv");

            case "pdf":
                var rowsT = data.Select(d => (d.Date, d.Orders, d.Units, d.Total));
                var totals = (header.Orders, header.Units, header.Total);
                var pdf = PdfReportBuilder.BuildSales("Reporte de ventas", f, t, rowsT, totals);
                return File(pdf, "application/pdf", $"sales_{f:yyyyMMdd}_{t:yyyyMMdd}.pdf");

            default:
                return Ok(new { header, rows = data });
        }
    }

    /*──────────────────────── Rotación de inventario ─────────────────
      Filtros extra:
      - days (default 30)
      - q: búsqueda por nombre o SKU
      - page / pageSize
      - format = json | csv | pdf
    ─────────────────────────────────────────────────────────────────*/
    [HttpGet("inventory-rotation")]
    public async Task<IActionResult> GetRotation(
        [FromQuery] int days = 30,
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 200,
        [FromQuery] string? format = "json")
    {
        days = days <= 0 ? 30 : days;
        page = page < 1 ? 1 : page;
        pageSize = Math.Clamp(pageSize, 10, 1000);

        var dateFrom = DateTime.UtcNow.AddDays(-days);

        // Promedio de salida diaria por material en el período
        var outs = await _ctx.MaterialMovements.AsNoTracking()
            .Where(m => m.MovementType == "O" && m.MovementDate >= dateFrom)
            .GroupBy(m => m.MaterialId)
            .Select(g => new { g.Key, AvgDailyOut = g.Sum(x => x.Quantity) / days })
            .ToDictionaryAsync(x => x.Key, x => x.AvgDailyOut);

        // Stock por material
        var stock = await _ctx.MaterialStocks.AsNoTracking()
            .GroupBy(s => s.MaterialId)
            .Select(g => new { g.Key, Qty = g.Sum(x => x.QuantityOnHand) })
            .ToListAsync();

        // Diccionario de materiales filtrado (si q)
        Dictionary<int, (string Name, string? SKU)> materialsDict;
        if (!string.IsNullOrWhiteSpace(q))
        {
            var lower = q.ToLower();
            materialsDict = await _ctx.Materials.AsNoTracking()
                .Where(m => (m.Name != null && m.Name.ToLower().Contains(lower)) ||
                            (m.SKU != null && m.SKU.ToLower().Contains(lower)))
                .Select(m => new { m.MaterialId, m.Name, m.SKU })
                .ToDictionaryAsync(m => m.MaterialId, m => (m.Name, m.SKU));
        }
        else
        {
            materialsDict = await _ctx.Materials.AsNoTracking()
                .Select(m => new { m.MaterialId, m.Name, m.SKU })
                .ToDictionaryAsync(m => m.MaterialId, m => (m.Name, m.SKU));
        }

        var all = stock
            .Where(s => materialsDict.ContainsKey(s.Key))
            .Select(s =>
            {
                outs.TryGetValue(s.Key, out var avg);
                var avgRounded = Math.Round(avg, 3);
                decimal? daysSupply = (avgRounded > 0) ? Math.Round(s.Qty / avgRounded, 1) : (decimal?)null;
                var m = materialsDict[s.Key];
                return new RotationDto(s.Key, m.Name, m.SKU, s.Qty, avgRounded, daysSupply);
            })
            .OrderBy(x => x.DaysSupply ?? decimal.MaxValue);

        var total = all.Count();
        var rows = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        switch ((format ?? "json").ToLowerInvariant())
        {
            case "csv":
                return Csv(
                    rows,
                    new[] { "MaterialId", "SKU", "Name", "StockQty", "AvgDailyOut", "DaysSupply" },
                    r => new[] {
                        r.MaterialId.ToString(),
                        r.SKU ?? "",
                        r.Name,
                        r.StockQty.ToString(CultureInfo.InvariantCulture),
                        r.AvgDailyOut.ToString(CultureInfo.InvariantCulture),
                        r.DaysSupply?.ToString(CultureInfo.InvariantCulture) ?? ""
                    },
                    $"rotation_last{days}d.csv"
                );

            case "pdf":
                var pdfRows = rows.Select(r => (r.MaterialId, r.SKU ?? "", r.Name, r.StockQty, r.AvgDailyOut, r.DaysSupply));
                var pdf = PdfReportBuilder.BuildRotation("Rotación de inventario", days, pdfRows);
                return File(pdf, "application/pdf", $"rotation_{days}d.pdf");

            default:
                return Ok(new { total, page, pageSize, rows });
        }
    }

    /*──────────────────────── Costos & márgenes ──────────────────────
      Filtros extra:
      - productId?
      - format = json | csv | pdf
    ─────────────────────────────────────────────────────────────────*/
    [HttpGet("costs-margins")]
    public async Task<IActionResult> GetCostsMargins(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? productId,
        [FromQuery] string? format = "json")
    {
        var (f, t) = NormalizeRange(from, to);

        var sales = _ctx.OrderLines.AsNoTracking()
            .Include(l => l.Order)
            .Where(l => l.Order.Status != "Cancelled" && l.Order.OrderDate >= f && l.Order.OrderDate <= t);

        if (productId.HasValue)
            sales = sales.Where(l => l.ProductId == productId.Value);

        var salesAgg = await sales
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, AvgPrice = g.Average(x => x.UnitPrice) })
            .ToDictionaryAsync(x => x.ProductId, x => x.AvgPrice);

        var productsQ = _ctx.Products.AsNoTracking().Select(p => new { p.ProductId, p.Name, p.BasePrice });
        if (productId.HasValue) productsQ = productsQ.Where(p => p.ProductId == productId.Value);

        var products = await productsQ.ToListAsync();

        var rows = products.Select(p =>
        {
            salesAgg.TryGetValue(p.ProductId, out var avgSale);
            var avg = Math.Round(avgSale, 2);
            var margin = (avg > 0) ? Math.Round((avg - p.BasePrice) / avg * 100, 2) : 0m;
            return new MarginDto(p.ProductId, p.Name, Math.Round(p.BasePrice, 2), avg, margin);
        })
        .OrderByDescending(x => x.Margin)
        .ToList();

        switch ((format ?? "json").ToLowerInvariant())
        {
            case "csv":
                return Csv(
                    rows,
                    new[] { "ProductId", "Name", "BasePrice", "AvgSalePrice", "MarginPct" },
                    r => new[] {
                        r.ProductId.ToString(),
                        r.Name,
                        r.BasePrice.ToString(CultureInfo.InvariantCulture),
                        r.AvgSalePrice.ToString(CultureInfo.InvariantCulture),
                        r.Margin.ToString(CultureInfo.InvariantCulture)
                    },
                    $"margins_{f:yyyyMMdd}_{t:yyyyMMdd}.csv"
                );

            case "pdf":
                var pdfRows = rows.Select(r => (r.ProductId, r.Name, r.BasePrice, r.AvgSalePrice, r.Margin));
                var pdf = PdfReportBuilder.BuildMargins("Márgenes por producto", f, t, pdfRows);
                return File(pdf, "application/pdf", $"margins_{f:yyyyMMdd}_{t:yyyyMMdd}.pdf");

            default:
                return Ok(new { from = f, to = t, rows });
        }
    }

    /*──────────────────────── Totales de ventas (resumen) ────────────*/
    [HttpGet("sales/summary")]
    public async Task<IActionResult> GetSalesSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to,
                                                     [FromQuery] string? customerId, [FromQuery] int? productId)
    {
        var (f, t) = NormalizeRange(from, to);

        var q = _ctx.Orders.AsNoTracking()
            .Include(o => o.OrderLines)
            .Where(o => o.Status != "Cancelled" && o.OrderDate >= f && o.OrderDate <= t);

        if (!string.IsNullOrWhiteSpace(customerId))
            q = q.Where(o => o.CustomerId == customerId);

        if (productId.HasValue)
            q = q.Where(o => o.OrderLines.Any(l => l.ProductId == productId.Value));

        var orders = await q.CountAsync();
        var units = await q.SelectMany(o => o.OrderLines).SumAsync(l => l.Quantity);
        var total = await q.SumAsync(o => o.TotalAmount);

        return Ok(new { from = f, to = t, orders, units, total = Math.Round(total, 2) });
    }
}

/*──────────────────────── DTOs (si no existen ya) ───────────────────*/
public record SalesReportLineDto(DateTime Date, int Orders, decimal Units, decimal Total);
public record RotationDto(int MaterialId, string Name, string? SKU, decimal StockQty, decimal AvgDailyOut, decimal? DaysSupply);
public record MarginDto(int ProductId, string Name, decimal BasePrice, decimal AvgSalePrice, decimal Margin);
