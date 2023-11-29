namespace Turbo.Maui.Services.Models;

public interface IBluetoothAdapter
{
    IConnectedDevice ConnectedDevice { get; }
    Task<bool> StartScanningForDevices(string[] uuids = null);
    void StopScanningForDevices();
    void ConnectTo(string address);
    void DisconnectDevice();
    event EventHandler<EventDataArgs<Packet>> DeviceDiscovered;
    event EventHandler<EventDataArgs<BLEDeviceStatus>> DeviceConnectionStatus;
    event EventHandler<EventArgs> BluetoothStateChanged;
    bool IsScanning { get; }
    bool IsPoweredOn { get; }
    bool CanAccess { get; }
}