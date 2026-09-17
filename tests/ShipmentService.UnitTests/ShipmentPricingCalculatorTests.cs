using ShipmentService.Application.Pricing;

namespace ShipmentService.UnitTests;

// BaseFee 5.00 + PerKgRate 1.50/kg, scaled by delivery-type multiplier (SameDay 2.5x, Express 1.75x, Standard 1x).
public class ShipmentPricingCalculatorTests
{
    private readonly ShipmentPricingCalculator _sut = new();

    [Theory]
    [InlineData(2, "Standard", 8.00)]   // (5 + 2*1.5) * 1.0 = 8.00
    [InlineData(2, "Express", 14.00)]   // (5 + 2*1.5) * 1.75 = 14.00
    [InlineData(2, "SameDay", 20.00)]   // (5 + 2*1.5) * 2.5 = 20.00
    [InlineData(0, "Standard", 5.00)]   // (5 + 0) * 1.0 = 5.00
    [InlineData(10, "SameDay", 50.00)]  // (5 + 10*1.5) * 2.5 = 50.00
    public void Calculate_AppliesBaseFeePerKgRateAndDeliveryTypeMultiplier(decimal weightKg, string deliveryType, decimal expected)
    {
        var result = _sut.Calculate(weightKg, deliveryType);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Calculate_UnknownDeliveryType_FallsBackToStandardMultiplier()
    {
        var result = _sut.Calculate(2, "SomethingUnrecognized");

        Assert.Equal(8.00m, result);
    }
}
