namespace Turbo.Maui.Services.Models;

public class DeviceConfiguration
{
    //public BLEConfiguration Ble { get; set; }
    public GeoConfiguration Geo { get; set; }
    public SecurityConfiguration Security { get; set; }

    public int? DisconnectReg { get; set; }
    public uint DisconnectVal { get; set; }

    public int? ConnectionTimestampReg { get; set; } //Register to send a requested timestamp to on connection
}