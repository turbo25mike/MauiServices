using System.Diagnostics;

namespace Turbo.Maui.Services;

public interface IBluetoothService
{
    IConnectedDevice ConnectedDevice { get; }
    void Stop();
    void Scan(string[] uuids = null, int? manufacturerID = null);
    void ConnectTo(string deviceID);
    Task StartNotifying(string service, string characteristic);
    void StopNotifying(string service, string characteristic);
    Task<int> RequestMTU(int size);
    Task<IBLERequest> Write(IBLERequest request);
    Task<IBLERequest> Read(IBLERequest request);
    void DisconnectDevice();
    event EventHandler<EventDataArgs<Packet>> PacketDiscovered;
    event EventHandler<EventDataArgs<BLEDeviceStatus>> DeviceConnectionStatus;
    event EventHandler<EventArgs> Stopped;
    event EventHandler<EventDataArgs<Tuple<string, string, byte[]>>> NotificationReceived;
    event EventHandler<EventArgs> BluetoothStateChanged;
    bool IsScanning { get; }
    bool HasAccess { get; }
    bool IsPoweredOn { get; }
    void StateUpdated();
}

public class BluetoothService : IBluetoothService
{
    public BluetoothService(IBluetoothAdapter adapter)
    {
        Debug.WriteLine("BluetoothSevice Init");
        _Adapter = adapter;
        _Adapter.BluetoothStateChanged += Adapter_BluetoothStateChanged;
        _Adapter.DeviceDiscovered += DeviceDiscovered;
        _Adapter.DeviceConnectionStatus += Adapter_DeviceConnectionStatus;
    }

    #region Public Methods

    /// <summary>
    /// Stops scanning for advertisements
    /// </summary>
    public void Stop() => _Adapter.StopScanningForDevices();

    public void Scan(string[] uuids = null, int? manufacturerID = null)
    {
        _Adapter.StartScanningForDevices(uuids, manufacturerID);

        if (!_Adapter.IsScanning)
            Stopped?.Invoke(this, new());
    }

    public void ConnectTo(string deviceID) => _Adapter.ConnectTo(deviceID);

    public void DisconnectDevice() => _Adapter.DisconnectDevice();
    
    public async Task<int> RequestMTU(int size)
    {
        var result = await ConnectedDevice.RequestMTU(size);
        return (int)result;
    }

    public Task<IBLERequest> Write(IBLERequest request) => ConnectedDevice.Write(request);

    public Task<IBLERequest> Read(IBLERequest request) => ConnectedDevice.Read(request);

    public async Task StartNotifying(string service, string characteristic) => await ConnectedDevice.StartNotifying(service, characteristic);
    
    private void ConnectedDevice_CharacteristicChanged(object sender, EventDataArgs<Tuple<string, string, byte[]>> e)
    {
        NotificationReceived?.Invoke(this, e);
    }

    public void StopNotifying(string service, string characteristic) => ConnectedDevice.StopNotifying(service, characteristic);

    #endregion

    #region Private Methods

    private void Adapter_DeviceConnectionStatus(object sender, EventDataArgs<BLEDeviceStatus> e)
    {
        switch (e.Data)
        {
            case BLEDeviceStatus.Connected:
                Debug.WriteLine($"BluetoothService->Adapter_DeviceConnectionStatus: Connected");
                ConnectedDevice.CharacteristicChanged -= ConnectedDevice_CharacteristicChanged;
                ConnectedDevice.CharacteristicChanged += ConnectedDevice_CharacteristicChanged;
                if (ConnectedDevice.GattReady)
                    DeviceReady(this, new());
                else
                {
                    ConnectedDevice.DeviceReady -= DeviceReady;
                    ConnectedDevice.DeviceReady += DeviceReady;
                }
                break;
            case BLEDeviceStatus.Disconnected:
                Debug.WriteLine($"BluetoothService->Adapter_DeviceConnectionStatus: Disconnected");
                DeviceConnectionStatus?.Invoke(this, e);
                break;
        }
    }

    private void DeviceReady(object? s, EventArgs e)
    {
        Debug.WriteLine($"BluetoothService->DeviceReady");
        DeviceConnectionStatus?.Invoke(this, new(BLEDeviceStatus.Connected));
    }

    private void DeviceDiscovered(object sender, EventDataArgs<Packet> e) => PacketDiscovered?.Invoke(this, e);

    //Specific to Android
    //Android bubbles an event through the BLEBroadcastReciever
    public void StateUpdated() => BluetoothStateChanged?.Invoke(this, new EventArgs());

    //Specific to iOS
    //iOS bubbles an event through the BLEAdapter
    private void Adapter_BluetoothStateChanged(object sender, EventArgs e) => BluetoothStateChanged?.Invoke(this, new EventArgs());

    #endregion

    #region Properties

    public IConnectedDevice ConnectedDevice => _Adapter.ConnectedDevice;

    public event EventHandler<EventDataArgs<Packet>> PacketDiscovered;
    public event EventHandler<EventArgs> Stopped;
    public event EventHandler<EventArgs> BluetoothStateChanged;
    public event EventHandler<EventDataArgs<BLEDeviceStatus>> DeviceConnectionStatus;
    public event EventHandler<EventDataArgs<Tuple<string, string, byte[]>>> NotificationReceived;
    public bool IsScanning => _Adapter.IsScanning;
    public bool HasAccess => _Adapter.CanAccess;
    public bool IsPoweredOn => _Adapter.IsPoweredOn;

    readonly IBluetoothAdapter _Adapter;

    #endregion
}

public class DeviceMessageBytesEventArgs
{
    public byte[] Response { get; private set; }
    public DeviceMessageBytesEventArgs(byte[] response) { Response = response; }
}