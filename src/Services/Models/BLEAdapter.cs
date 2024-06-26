﻿namespace Turbo.Maui.Services.Platforms;

///// <summary>
///// This is for .net Test build only
///// </summary>
public partial class BLEAdapter : IBluetoothAdapter
{
#if __IOS__
#elif __ANDROID__
#elif WINDOWS
#else
    public IConnectedDevice? ConnectedDevice => throw new NotImplementedException();

    public bool IsScanning => throw new NotImplementedException();

    public bool IsPoweredOn => throw new NotImplementedException();

    public bool CanAccess => throw new NotImplementedException();

    public bool IsAdvertising => throw new NotImplementedException();

    public void ConnectTo(string address) => throw new NotImplementedException();

    public void DisconnectDevice() => throw new NotImplementedException();

    public bool StartScanningForDevices(string[]? uuids = null, int? manufacturerID = null) => throw new NotImplementedException();

    public void StopScanningForDevices() => throw new NotImplementedException();

    public void StartAdvertising(BLEAdvertisingManager manager)
    {
        throw new NotImplementedException();
    }

    public void StopAdvertising()
    {
        throw new NotImplementedException();
    }

    public void Notify(string serviceID, string characteristicID, string value)
    {
        throw new NotImplementedException();
    }

    public event EventHandler<EventDataArgs<Packet>>? DeviceDiscovered;
    public event EventHandler<EventDataArgs<BLEDeviceStatus>>? DeviceConnectionStatus;
    public event EventHandler<EventArgs>? BluetoothStateChanged;
#endif
}