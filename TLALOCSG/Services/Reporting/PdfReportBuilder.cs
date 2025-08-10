using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace TLALOCSG.Services.Reporting;

public static class PdfReportBuilder
{
    /* Utilidad para estilizar cabeceras */
    private static IContainer HeaderCell(IContainer c) => c.Background(Colors.Grey.Lighten3).Padding(6);

    /* ───────── Ventas ───────── */
    public static byte[] BuildSales(
        string title, DateTime from, DateTime to,
        IEnumerable<(DateTime date, int orders, decimal units, decimal total)> rows,
        (int orders, decimal units, decimal total) totals)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Margin(30);

                page.Header().Text($"{title}  ({from:yyyy-MM-dd} .. {to:yyyy-MM-dd})")
                    .SemiBold().FontSize(16);

                page.Content().Table(t =>
                {
                    t.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(2); // Fecha
                        cols.RelativeColumn();  // Órdenes
                        cols.RelativeColumn();  // Unidades
                        cols.RelativeColumn();  // Total
                    });

                    t.Header(h =>
                    {
                        h.Cell().Element(HeaderCell).Text("Fecha");
                        h.Cell().Element(HeaderCell).Text("Órdenes");
                        h.Cell().Element(HeaderCell).Text("Unidades");
                        h.Cell().Element(HeaderCell).Text("Total");
                    });

                    foreach (var r in rows)
                    {
                        t.Cell().Padding(6).Text(r.date.ToString("yyyy-MM-dd"));
                        t.Cell().Padding(6).AlignRight().Text(r.orders.ToString());
                        t.Cell().Padding(6).AlignRight().Text(r.units.ToString(CultureInfo.InvariantCulture));
                        t.Cell().Padding(6).AlignRight().Text(r.total.ToString("C", CultureInfo.CurrentCulture));
                    }

                    // Totales
                    t.Cell().PaddingTop(8).Text(string.Empty);
                    t.Cell().PaddingTop(8).AlignRight().Text(totals.orders.ToString()).SemiBold();
                    t.Cell().PaddingTop(8).AlignRight().Text(totals.units.ToString(CultureInfo.InvariantCulture)).SemiBold();
                    t.Cell().PaddingTop(8).AlignRight().Text(totals.total.ToString("C", CultureInfo.CurrentCulture)).SemiBold();
                });

                page.Footer().AlignRight().Text(txt =>
                {
                    txt.Span("Generado ").FontSize(9);
                    txt.Span(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'")).FontSize(9).SemiBold();
                });
            });
        }).GeneratePdf();
    }

    /* ───────── Rotación ───────── */
    public static byte[] BuildRotation(
        string title, int days,
        IEnumerable<(int id, string sku, string name, decimal stock, decimal avg, decimal? daysSupply)> rows)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Margin(30);

                page.Header().Text($"{title} (últimos {days} días)")
                    .SemiBold().FontSize(16);

                page.Content().Table(t =>
                {
                    t.ColumnsDefinition(col =>
                    {
                        col.RelativeColumn(2); // Material
                        col.RelativeColumn();  // SKU
                        col.RelativeColumn();  // Stock
                        col.RelativeColumn();  // Salida diaria
                        col.RelativeColumn();  // Días cobertura
                        col.RelativeColumn();  // Estado
                    });

                    t.Header(h =>
                    {
                        h.Cell().Element(HeaderCell).Text("Material");
                        h.Cell().Element(HeaderCell).Text("SKU");
                        h.Cell().Element(HeaderCell).Text("Stock");
                        h.Cell().Element(HeaderCell).Text("Salida diaria");
                        h.Cell().Element(HeaderCell).Text("Días cobertura");
                        h.Cell().Element(HeaderCell).Text("Estado");
                    });

                    foreach (var r in rows)
                    {
                        var state = r.daysSupply is null ? "N/A"
                                   : (r.daysSupply < 5 ? "Crítico" : (r.daysSupply <= 15 ? "Medio" : "OK"));

                        t.Cell().Padding(6).Text(r.name);
                        t.Cell().Padding(6).Text(r.sku);
                        t.Cell().Padding(6).AlignRight().Text(r.stock.ToString(CultureInfo.InvariantCulture));
                        t.Cell().Padding(6).AlignRight().Text(r.avg.ToString(CultureInfo.InvariantCulture));
                        t.Cell().Padding(6).AlignRight().Text(r.daysSupply?.ToString(CultureInfo.InvariantCulture) ?? "N/A");
                        t.Cell().Padding(6).Text(state);
                    }
                });
            });
        }).GeneratePdf();
    }

    /* ───────── Márgenes ───────── */
    public static byte[] BuildMargins(
        string title, DateTime from, DateTime to,
        IEnumerable<(int id, string name, decimal basePrice, decimal avgPrice, decimal margin)> rows)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Margin(30);

                page.Header().Text($"{title}  ({from:yyyy-MM-dd} .. {to:yyyy-MM-dd})")
                    .SemiBold().FontSize(16);

                page.Content().Table(t =>
                {
                    t.ColumnsDefinition(col =>
                    {
                        col.RelativeColumn(2); // Producto
                        col.RelativeColumn();  // Costo base
                        col.RelativeColumn();  // Precio medio
                        col.RelativeColumn();  // Margen %
                    });

                    t.Header(h =>
                    {
                        h.Cell().Element(HeaderCell).Text("Producto");
                        h.Cell().Element(HeaderCell).Text("Costo base");
                        h.Cell().Element(HeaderCell).Text("Precio medio");
                        h.Cell().Element(HeaderCell).Text("Margen %");
                    });

                    foreach (var r in rows)
                    {
                        t.Cell().Padding(6).Text(r.name);
                        t.Cell().Padding(6).AlignRight().Text(r.basePrice.ToString("C", CultureInfo.CurrentCulture));
                        t.Cell().Padding(6).AlignRight().Text(r.avgPrice.ToString("C", CultureInfo.CurrentCulture));
                        t.Cell().Padding(6).AlignRight().Text(r.margin.ToString("0.##", CultureInfo.InvariantCulture));
                    }
                });
            });
        }).GeneratePdf();
    }
}
