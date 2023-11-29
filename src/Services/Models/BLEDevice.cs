using System.Text.Json.Serialization;
using SQLite;

namespace Turbo.Maui.Services.Models;

public interface IBLEDevice
{
    [JsonIgnore]
    [Ignore]
    DeviceState State { get; set; }

    [JsonIgnore]
    [Ignore]
    DeviceConfiguration Config { get; }
}

public abstract class BLEDevice : IBLEDevice
{
    public abstract DeviceState State { get; set; }

    public abstract DeviceConfiguration Config { get; }
}