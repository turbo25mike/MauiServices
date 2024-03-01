using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography;

namespace Turbo.Maui.Services.Platforms;

public class ConnectedDevice : IConnectedDevice
{
    public ConnectedDevice(BluetoothLEDevice device, GattSession gatt, Dictionary<GattDeviceService, IEnumerable<GattCharacteristic>> services)
    {
        _Services = services;
        _Device = device;
        _Gatt = gatt;
        _Gatt.MaxPduSizeChanged += Gatt_MaxPduSizeChanged;
        CharacteristicsDiscovered = true;
        GattReady = true;
        DeviceReady?.Invoke(this, new());
        MTU = 20;
    }

    //Destructor
    ~ConnectedDevice() => Dispose();

    public Task<nuint> RequestMTU(int size)
    {
        MTU = _Gatt.MaxPduSize;
        return Task.FromResult(MTU);
    }


    private void Gatt_MaxPduSizeChanged(object? sender, object e) => MTU = _Gatt.MaxPduSize;

    public async void Write(string serviceID, string characteristicID, byte[] val, bool withResponse = true)
    {
        var ch = GetCharacteristic(serviceID, characteristicID);

        _WriteInProgress = true;
        var writeType = withResponse ? GattWriteOption.WriteWithResponse : GattWriteOption.WriteWithoutResponse;
        await ch.WriteValueAsync(CryptographicBuffer.CreateFromByteArray(val), writeType);
    }

    public async void Read(string serviceID, string characteristicID, Action<KeyValuePair<string, byte[]?>> action, bool notify)
    {
        try
        {
            var deviceId = BLEUtils.ParseDeviceId(_Device.BluetoothAddress).ToString();
            var ch = GetCharacteristic(serviceID, characteristicID);

            if (notify)
            {
                ch.ValueChanged += (s, e) => action.Invoke(new KeyValuePair<string, byte[]?>(deviceId, e.CharacteristicValue?.ToArray()));
                var result = await ch.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
            }
            else
            {
                var readResult = await ch.ReadValueAsync(BluetoothCacheMode.Uncached);
                action.Invoke(new KeyValuePair<string, byte[]?>(_Device.DeviceId, readResult.Value?.ToArray()));
            }
        }catch(Exception ex)
        {

        }
    }

    public void Dispose()
    {
        if (_Device.ConnectionStatus == BluetoothConnectionStatus.Disconnected || _Device.GattServices is null) return;
        foreach (var service in _Device.GattServices)
        {
            service.Session?.Dispose();
            service.Dispose();
        }
    }

    public async void StopNotifying(string serviceID, string characteristicID)
    {
        var ch = GetCharacteristic(serviceID, characteristicID);
        if (ch is null) return;
        await ch.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
    }

    public IEnumerable<BLEService> GetServices() =>
        _Services.Select(s => new BLEService(s.Key.Uuid.ToString().ToUpper(), s.Value.Select(c => new BLECharacteristic(c.Uuid.ToString().ToUpper(), c.UserDescription))));

    private KeyValuePair<GattDeviceService, IEnumerable<GattCharacteristic>>? GetService(string uuid) =>
        _Services.FirstOrDefault(s => s.Key.Uuid.ToString().Equals(uuid, StringComparison.CurrentCultureIgnoreCase));
    
    private GattCharacteristic GetCharacteristic(string serviceID, string uuid)
    {
        var service = GetService(serviceID);
        var ch = service?.Value.FirstOrDefault(s => s.Uuid.ToString().Equals(uuid, StringComparison.CurrentCultureIgnoreCase));
        return ch;
    }
        
    private Dictionary<GattDeviceService, IEnumerable<GattCharacteristic>> _Services;

    public bool CanSendWriteWithoutResponse() => !_WriteInProgress;
    public bool CanSendWrite() => !_WriteInProgress;
    public bool HasService(string serviceID) => GetService(serviceID) != null;
    public bool HasCharacteristic(string serviceID, string characteristicID) => GetCharacteristic(serviceID, characteristicID) != null;

    public bool GattReady { get; private set; }
    public bool CharacteristicsDiscovered { get; private set; }
    public string Address { get => BLEUtils.ParseDeviceId(_Device.BluetoothAddress).ToString(); }

    public nuint MTU { get; private set; }
    public event EventHandler<EventDataArgs<string[]>>? ServicesDiscovered = delegate { };
    public event EventHandler? IsReadyToSendWriteWithoutResponse;
    public event EventHandler? CharacteristicWrite;
    public event EventHandler? DeviceReady;

    readonly GattSession _Gatt;
    readonly BluetoothLEDevice _Device;
    private bool _WriteInProgress = false;
}