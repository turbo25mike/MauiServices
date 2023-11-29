namespace Turbo.Maui.Services.Tests.TestExtentions;

public static class PinTestExt
{
    public static Microsoft.Maui.Devices.Sensors.Location PinAccuracy(this IEnumerable<Microsoft.Maui.Devices.Sensors.Location> data)
    {
        var filter = new KalmanFilter();
        Microsoft.Maui.Devices.Sensors.Location result = new();
        foreach (var p in data)
            result = filter.Process(p);
        return result;
    }

    public static void PinShouldMatch(this Pin actual, Pin expected)
    {
        actual.Latitude.Should().Be(expected.Latitude);
        actual.Longitude.Should().Be(expected.Longitude);
    }
}