using CommunityToolkit.Mvvm.ComponentModel;

namespace Turbo.Maui.Services;

public partial class Pin : ObservableObject
{
    public Pin()
    {
        Timestamp = DateTime.Now;
    }

    public Pin(string latlng)
    {
        var data = latlng.Trim('(').Trim(')').Split(',');

        var latData = data[0].Split('°');
        _ = double.TryParse(latData[0], out _Latitude);

        if (latData.Length > 1 && latData[1].Trim().ToLower() == "s")
            _Latitude *= -1;

        var lngData = data[1].Split('°');

        _ = double.TryParse(lngData[0], out _Longitude);

        if (lngData.Length > 1 && lngData[1].Trim().ToLower() == "w")
            _Longitude *= -1;
    }

    public void Update(double lat, double lng, double? acc = 0) { Latitude = lat; Longitude = lng; Accuracy = acc; }
    public void Update(Pin p) { Latitude = p.Latitude; Longitude = p.Longitude; Accuracy = p.Accuracy; }

    [ObservableProperty]
    private DateTime _Timestamp;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LatLong))]
    private double _Latitude;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LatLong))]
    private double _Longitude;

    [ObservableProperty]
    private double? _Accuracy;

    public string LatLong => (Latitude == 0 || Longitude == 0) ? "" : $"({Truncate(Latitude)},{Truncate(Longitude)})";

    private static double Truncate(double d, int digits = 7)
    {
        var stepper = Math.Pow(10, digits);
        int temp = (int)(stepper * d);
        return temp / stepper;
    }
}