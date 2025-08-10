using Microsoft.EntityFrameworkCore;
using TLALOCSG.Data;
using TLALOCSG.DTOs.Quotes;

namespace TLALOCSG.Services.Quotes;

public interface IQuotePricingService
{
    Task<QuotePricePreviewDto> CalculateAsync(int quoteId, QuoteOptionsDto opts);
}

public class QuotePricingService : IQuotePricingService
{
    private readonly IoTIrrigationDbContext _ctx;
    public QuotePricingService(IoTIrrigationDbContext ctx) => _ctx = ctx;

    public async Task<QuotePricePreviewDto> CalculateAsync(int quoteId, QuoteOptionsDto opts)
    {
        var q = await _ctx.Quotes
            .Include(x => x.QuoteLines).ThenInclude(l => l.Product)
            .FirstAsync(x => x.QuoteId == quoteId);

        var qtyTotal = q.QuoteLines.Sum(l => (int)l.Quantity);
        var products = q.QuoteLines.Sum(l => l.Quantity * (l.Product!.BasePrice));

        decimal installBase = 0, transport = 0, shipping = 0;

        // Normaliza fulfillment
        var f = (opts.Fulfillment ?? "").Trim();
        if (f is not ("DevicesOnly" or "Shipping" or "Installation"))
            throw new ArgumentException("Fulfillment inválido. Use DevicesOnly, Shipping o Installation.");

        // Helper para estado
        static string? Norm(string? code)
            => string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();

        var stateCode = Norm(opts.StateCode);

        if (f == "Installation")
        {
            // Base por tier (siempre que haya productos)
            if (qtyTotal > 0)
            {
                var tier = await _ctx.InstallTiers
                    .OrderBy(t => t.MinQty)
                    .FirstAsync(t => qtyTotal >= t.MinQty && (t.MaxQty == null || qtyTotal <= t.MaxQty));
                installBase = tier.BaseCost;
            }

            // Transporte si NO es GTO y hay estado válido
            if (stateCode is not null && stateCode != "GTO")
            {
                var state = await _ctx.StateRates.AsNoTracking()
                               .FirstOrDefaultAsync(s => s.StateCode == stateCode);
                if (state is null)
                    throw new KeyNotFoundException($"Estado '{stateCode}' no encontrado.");

                var km = Math.Max(0, opts.ManualDistanceKm ?? state.DistanceKm);
                transport = km * state.TransportPerKm;
            }
            // En GTO, transporte = 0
        }
        else if (f == "Shipping")
        {
            // Requiere estado válido (en el front ya lo exigimos, aquí reforzamos)
            if (stateCode is null)
                throw new ArgumentException("Debe seleccionar un estado para el envío.");

            if (stateCode != "GTO")
            {
                var state = await _ctx.StateRates.AsNoTracking()
                               .FirstOrDefaultAsync(s => s.StateCode == stateCode);
                if (state is null)
                    throw new KeyNotFoundException($"Estado '{stateCode}' no encontrado.");

                var km = Math.Max(0, opts.ManualDistanceKm ?? state.DistanceKm);
                shipping = km * state.ShipPerKm; // GTO -> 0
            }
        }
        // DevicesOnly: extras = 0

        var grand = Math.Round(products + installBase + transport + shipping, 2);

        return new QuotePricePreviewDto(
            Math.Round(products, 2),
            Math.Round(installBase, 2),
            Math.Round(transport, 2),
            Math.Round(shipping, 2),
            grand
        );
    }
}
