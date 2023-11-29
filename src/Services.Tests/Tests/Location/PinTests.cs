namespace Turbo.Maui.Services.Tests;

public class PinTests
{
    [Fact]
    public void GoogleDataTests()
    {
        GivenGoogleData(Heading.NW).PinShouldMatch(SomePin);
        GivenGoogleData(Heading.NE).PinShouldMatch(SomePin);
        GivenGoogleData(Heading.SE).PinShouldMatch(SomePin);
        GivenGoogleData(Heading.SW).PinShouldMatch(SomePin);
    }

    [Fact]
    public void AppleTests()
    {
        GivenAppleData(Heading.NW).PinShouldMatch(SomePin);
        GivenAppleData(Heading.NE).PinShouldMatch(SomePin);
        GivenAppleData(Heading.SE).PinShouldMatch(SomePin);
        GivenAppleData(Heading.SW).PinShouldMatch(SomePin);
    }

    [Fact]
    public void OnXTests()
    {
        GivenOnXData(Heading.NW).PinShouldMatch(SomePin);
        GivenOnXData(Heading.NE).PinShouldMatch(SomePin);
        GivenOnXData(Heading.SE).PinShouldMatch(SomePin);
        GivenOnXData(Heading.SW).PinShouldMatch(SomePin);
    }

    #region Private

    private void BuildSomePin(Heading h)
    {
        var rnd = new Random();
        var lat = rnd.Next(0, 90) + rnd.NextDouble();
        if (h == Heading.SW || h == Heading.SE)
            lat *= -1;

        var lng = rnd.Next(0, 180) + rnd.NextDouble();
        if (h == Heading.NW || h == Heading.SW)
            lat *= -1;

        SomePin = new() { Latitude = lat, Longitude = lng };
    }

    private Pin GivenOnXData(Heading h)
    {
        BuildSomePin(h);
        return new($"{SomePin.Latitude}, {SomePin.Longitude}");
    }

    private Pin GivenGoogleData(Heading h)
    {
        BuildSomePin(h);
        return new($"({SomePin.Latitude}, {SomePin.Longitude})");
    }

    private Pin GivenAppleData(Heading h)
    {
        BuildSomePin(h);
        var latHeading = "N";
        var lat = SomePin.Latitude;
        var lngHeading = "E";
        var lng = SomePin.Longitude;

        if (SomePin.Latitude < 0)
        {
            latHeading = "S";
            lat *= -1;
        }
        if (SomePin.Longitude < 0)
        {
            lngHeading = "W";
            lng *= -1;
        }

        return new($"{lat}° {latHeading}, {lng}° {lngHeading}");
    }

    private Pin SomePin = new();

    public enum Heading { NW, NE, SW, SE }

    #endregion
}