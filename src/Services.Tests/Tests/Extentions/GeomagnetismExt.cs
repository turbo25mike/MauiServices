namespace Turbo.Maui.Services.Tests.TestExtentions;

public static class GeomagnetismExt
{
    public static void Expected(this Geomagnetism given, double dec, double inc, double hInt, double vInt, double nInt, double eInt, double i, double within)
    {
        given.Declination.Should().BeApproximately(dec, within);
        given.Inclination.Should().BeApproximately(inc, within);
        given.HorizontalIntensity.Should().BeApproximately(hInt, within);
        given.VerticalIntensity.Should().BeApproximately(vInt, within);
        given.NorthIntensity.Should().BeApproximately(nInt, within);
        given.EastIntensity.Should().BeApproximately(eInt, within);
        given.Intensity.Should().BeApproximately(i, within);
    }

    public static double AndDirectionIs(this Geomagnetism given, double dir)
    {
        return given.GetTrueDirection(dir);
    }
}