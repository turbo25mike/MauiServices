using CommunityToolkit.Mvvm.ComponentModel;

namespace Turbo.Maui.Services.Models;

public partial class Packet : ObservableObject
{
    [ObservableProperty]
    private string _ID;
    [ObservableProperty]
    private string _Name;
    [ObservableProperty]
    private int _RSSI;
    [ObservableProperty]
    private int _TxPower;

    public double Distance
    {
        get
        {
            if (RSSI == 0)
                return -1.0; // if we cannot determine distance, return -1.
            var ratio = RSSI * 1.0 / TxPower;
            return ratio < 1.0 ? Math.Pow(ratio, 10) : 0.89976 * Math.Pow(ratio, 7.7095) + 0.111;
        }
    }

    public byte[] ServiceData { get; set; }
    public byte[] ManufacturerData { get; set; }
}