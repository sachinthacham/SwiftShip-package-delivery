using DriverService.Application.Common;

namespace DriverService.UnitTests;

public class GeoDistanceCalculatorTests
{
    [Fact]
    public void HaversineKm_ReturnsZero_ForIdenticalPoints()
    {
        var distance = GeoDistanceCalculator.HaversineKm(40.0, -75.0, 40.0, -75.0);

        Assert.Equal(0, distance, precision: 6);
    }

    [Fact]
    public void HaversineKm_ReturnsExpectedDistance_BetweenKnownCities()
    {
        // New York City to Los Angeles is approximately 3936 km great-circle distance.
        var distance = GeoDistanceCalculator.HaversineKm(40.7128, -74.0060, 34.0522, -118.2437);

        Assert.InRange(distance, 3900, 3970);
    }

    [Fact]
    public void HaversineKm_IsSymmetric()
    {
        var a = GeoDistanceCalculator.HaversineKm(51.5074, -0.1278, 48.8566, 2.3522);
        var b = GeoDistanceCalculator.HaversineKm(48.8566, 2.3522, 51.5074, -0.1278);

        Assert.Equal(a, b, precision: 9);
    }
}
