using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography;

namespace Turbo.Maui.Services.Platforms;

public class ConnectedDevice : IConnectedDevice
{
    public ConnectedDevice(string address, BluetoothLEDevice device, GattSession gatt)
    {
        Address = address;
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
        var ch = GetCharacteristic(GetService(serviceID), characteristicID);

        _WriteInProgress = true;
        var writeType = withResponse ? GattWriteOption.WriteWithResponse : GattWriteOption.WriteWithoutResponse;
        await ch.WriteValueAsync(CryptographicBuffer.CreateFromByteArray(val), writeType);
    }

    public async void Read(string serviceID, string characteristicID, Action<KeyValuePair<string, byte[]?>> action, bool notify)
    {
        var ch = GetCharacteristic(GetService(serviceID), characteristicID);
        if (notify)
        {
            ch.ValueChanged += (s, e) => action.Invoke(new KeyValuePair<string, byte[]?>(_Device.DeviceId, e.CharacteristicValue?.ToArray()));
            await ch.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
        }
        else
        {
            var readResult = await ch.ReadValueAsync(BluetoothCacheMode.Uncached);
            action.Invoke(new KeyValuePair<string, byte[]?>(_Device.DeviceId, readResult.Value?.ToArray()));
        }
    }

    public void Dispose()
    {
        if (_Device.GattServices is null) return;
        foreach (var service in _Device.GattServices)
        {
            service.Session?.Dispose();
            service.Dispose();
        }
    }

    public async void StopNotifying(string serviceID, string characteristicID)
    {
        var ch = GetCharacteristic(GetService(serviceID), characteristicID);
        if (ch is null) return;
        await ch.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
    }

    public IEnumerable<BLEService> GetServices()
    {
        return _Device.GattServices?
            .Select(s => new BLEService(s.Uuid.ToString().ToUpper(), s.GetAllCharacteristics().Select(c => new BLECharacteristic(c.Uuid.ToString().ToUpper(), c.UserDescription)))) ?? new List<BLEService>();
    }

    private GattDeviceService GetService(string uuid)
    {
        var s = _Device.GattServices?.FirstOrDefault(s => s.Uuid.ToString().Equals(uuid, StringComparison.CurrentCultureIgnoreCase));
        return s is null ? throw new ArgumentNullException($"Service ({uuid}) not found.") : s;
    }
    private static GattCharacteristic GetCharacteristic(GattDeviceService service, string uuid)
    {
        var ch = service.GetAllCharacteristics().FirstOrDefault(s => s.Uuid.ToString().Equals(uuid, StringComparison.CurrentCultureIgnoreCase));
        return ch is null ? throw new ArgumentNullException($"Characteristic ({uuid}) not found.") : ch;
    }

    public bool CanSendWriteWithoutResponse() => !_WriteInProgress;
    public bool CanSendWrite() => !_WriteInProgress;
    public bool HasService(string serviceID) => GetService(serviceID) != null;
    public bool HasCharacteristic(string serviceID, string characteristicID) => GetCharacteristic(GetService(serviceID), characteristicID) != null;

    public bool GattReady { get; private set; }
    public bool CharacteristicsDiscovered { get; private set; }
    public string Address { get; private set; }

    public nuint MTU { get; private set; }
    public event EventHandler<EventDataArgs<string[]>>? ServicesDiscovered = delegate { };
    public event EventHandler? IsReadyToSendWriteWithoutResponse;
    public event EventHandler? CharacteristicWrite;
    public event EventHandler? DeviceReady;

    readonly GattSession _Gatt;
    readonly BluetoothLEDevice _Device;
    private bool _WriteInProgress = false;
}