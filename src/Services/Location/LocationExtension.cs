namespace Turbo.Maui.Services;

public static class LocationExt
{
    public static Pin ToPin(this Location l) => new() { Latitude = l?.Latitude ?? 0, Longitude = l?.Longitude ?? 0, Accuracy = l?.Accuracy ?? 0 };

    public static string GetLatLong(this Location l) => l.ToPin().LatLong;
}