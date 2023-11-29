namespace Turbo.Maui.Services.Tests;

public class KalmanFilterTests
{
    [Fact]
    public void SinglePointTest()
    {
        GivenSomePinData().PinAccuracy().Latitude.Should().BeApproximately(SomeResult.Latitude, WithIn);
    }

    #region Private

    private IEnumerable<Microsoft.Maui.Devices.Sensors.Location> GivenSomePinData()
    {
        AddPin(45.123456, 123.123456, 19);
        AddPin(45.123457, 123.123456, 19);
        AddPin(45.123459, 123.123456, 30);
        AddPin(45.123450, 123.123456, 15);
        AddPin(45.123456, 123.123456, 19);
        AddPin(45.123456, 123.123456, 10);
        AddPin(45.123459, 123.123456, 19);

        SomeResult = new Microsoft.Maui.Devices.Sensors.Location() { Latitude = 45.123456, Longitude = 123.123456, Accuracy = 10 };

        return _Pins;
    }

    private void AddPin(double lat, double lng, double acc)
    {
        var pin = new Microsoft.Maui.Devices.Sensors.Location() { Latitude = lat, Longitude = lng, Accuracy = acc };
        _Pins.Add(pin);
    }

    private double WithIn = .0005;
    private Microsoft.Maui.Devices.Sensors.Location SomeResult = new();
    private readonly List<Microsoft.Maui.Devices.Sensors.Location> _Pins = new();

    #endregion
}