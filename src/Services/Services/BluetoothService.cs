namespace Turbo.Maui.Services;

public interface IBluetoothService
{
    IConnectedDevice ConnectedDevice { get; }
    void Stop();
    void Scan(string[] uuids = null, int? manufacturerID = null);
    void ConnectTo(string deviceID);
    Task SetNotifications(string service, string characteristic);
    Task<int> RequestMTU(int size);
    void Write(IBLERequest request);
    Task WriteAsync(IBLERequest request);
    Task<IBLERequest> WriteAndNotifyAsync(IBLERequest request);
    void DisconnectDevice();
    Task IsReadyToSendWriteWithoutResponse();
    event EventHandler<EventDataArgs<Packet>> PacketDiscovered;
    event EventHandler<EventDataArgs<BLEDeviceStatus>> DeviceConnectionStatus;
    event EventHandler<EventArgs> Stopped;
    event EventHandler<DeviceMessageBytesEventArgs> DeviceMessageReceived;
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

    public void DisconnectDevice()
    {
        _IsFetchingData?.TrySetResult(null);
        _Adapter.DisconnectDevice();
    }

    public Task IsReadyToSendWriteWithoutResponse()
    {
        _IsReadyToSendWrite = new TaskCompletionSource();
        if (ConnectedDevice.CanSendWriteWithoutResponse())
            return Task.CompletedTask;
        return _IsReadyToSendWrite.Task;
    }

    public async Task<int> RequestMTU(int size)
    {
        var result = await ConnectedDevice.RequestMTU(size);
        return (int)result;
    }

    public Task CanSendWriteRequest()
    {
        _IsReadyToSendWrite = new TaskCompletionSource();
        if (ConnectedDevice.CanSendWriteWithoutResponse())
            return Task.CompletedTask;
        return _IsReadyToSendWrite.Task;
    }

    public void Write(IBLERequest request) => ConnectedDevice.Write(request.ServiceID, request.CharacteristicID, request.Data, request.WithResponse);

    public Task WriteAsync(IBLERequest request)
    {
        _IsReadyToSendWrite = new TaskCompletionSource();
        if (!ConnectedDevice.CanSendWrite())
            ConnectedDevice.CharacteristicWrite += ConnectedDevice_CharacteristicWrite;
        ConnectedDevice.Write(request.ServiceID, request.CharacteristicID, request.Data, request.WithResponse);
        return _IsReadyToSendWrite.Task;
    }

    private void ConnectedDevice_CharacteristicWrite(object sender, EventArgs e)
    {
        ConnectedDevice.CharacteristicWrite -= ConnectedDevice_CharacteristicWrite;
        _IsReadyToSendWrite?.TrySetResult();
    }

    public Task<IBLERequest> WriteAndNotifyAsync(IBLERequest request)
    {
        _IsFetchingData = new TaskCompletionSource<IBLERequest>();
        _CurrentRequest = request;
        ConnectedDevice.Write(request.ServiceID, request.CharacteristicID, request.Data, request.WithResponse);
        return _IsFetchingData.Task;
    }

    public async Task SetNotifications(string service, string characteristic)
    {
        ConnectedDevice.Read(service, characteristic, (e) =>
        {
            if (e.Value == null) return;
            if (_CurrentRequest != null && _CurrentRequest.SetResponse(e.Value))
                _IsFetchingData.TrySetResult(_CurrentRequest);
            DeviceMessageReceived?.Invoke(this, new(e.Value));
        }, true);

#if ANDROID
        await Task.Delay(250);
#endif
    }

    #endregion

    #region Private Methods

    private void Adapter_DeviceConnectionStatus(object sender, EventDataArgs<BLEDeviceStatus> e)
    {
        switch (e.Data)
        {
            case BLEDeviceStatus.Connected:
                ConnectedDevice.IsReadyToSendWriteWithoutResponse += ConnectedDevice_IsReadyToSendWriteWithoutResponse;
                if (ConnectedDevice.GattReady)
                    DeviceReady();
                else
                    ConnectedDevice.DeviceReady += (s, ei) => DeviceReady();
                break;
            case BLEDeviceStatus.Disconnected:

                if (_IsFetchingData != null)
                    _IsFetchingData.TrySetResult(null);
                DeviceConnectionStatus?.Invoke(this, e);
                break;
        }
    }

    private void ConnectedDevice_IsReadyToSendWriteWithoutResponse(object sender, EventArgs e) => _IsReadyToSendWrite?.TrySetResult();

    private void DeviceReady() => DeviceConnectionStatus?.Invoke(this, new(BLEDeviceStatus.Connected));

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
    public event EventHandler<DeviceMessageBytesEventArgs> DeviceMessageReceived;
    public bool IsScanning => _Adapter.IsScanning;
    public bool HasAccess => _Adapter.CanAccess;
    public bool IsPoweredOn => _Adapter.IsPoweredOn;

    readonly IBluetoothAdapter _Adapter;

    IBLERequest _CurrentRequest;
    TaskCompletionSource<IBLERequest> _IsFetchingData;
    TaskCompletionSource _IsReadyToSendWrite;

    #endregion
}

public class DeviceMessageBytesEventArgs
{
    public byte[] Response { get; private set; }
    public DeviceMessageBytesEventArgs(byte[] response) { Response = response; }
}