namespace ShipmentService.Application.Pricing;

/// <summary>
/// Flat base fee + per-kg rate, scaled by a delivery-type multiplier. No live distance
/// calculation yet (addresses are geocoded but not routed) — a placeholder for a future
/// distance-aware pricing pass once map/geocoding integration lands.
/// </summary>
public class ShipmentPricingCalculator : IShipmentPricingCalculator
{
    private const decimal BaseFee = 5.00m;
    private const decimal PerKgRate = 1.50m;

    public decimal Calculate(decimal weightKg, string deliveryType)
    {
        var multiplier = deliveryType switch
        {
            "SameDay" => 2.5m,
            "Express" => 1.75m,
            _ => 1.0m
        };

        var cost = (BaseFee + weightKg * PerKgRate) * multiplier;
        return Math.Round(cost, 2, MidpointRounding.AwayFromZero);
    }
}
