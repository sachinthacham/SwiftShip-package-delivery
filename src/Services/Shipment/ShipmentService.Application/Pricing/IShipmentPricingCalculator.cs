namespace ShipmentService.Application.Pricing;

public interface IShipmentPricingCalculator
{
    decimal Calculate(decimal weightKg, string deliveryType);
}
