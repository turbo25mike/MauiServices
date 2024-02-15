using System.Diagnostics;
using Android.Bluetooth;
using Java.Util;

namespace Turbo.Maui.Services.Platforms;

public class ConnectedDevice : IConnectedDevice
{
    public ConnectedDevice(string address, BluetoothDevice device, BluetoothGatt gatt, GattCallback callback)
    {
        Address = address;
        _Device = device;
        _Gatt = gatt;
        _GattCallback = callback;
        _GattCallback.CharacteristicWrite += (s, e) =>
        {
            _WriteInProgress = false;
            CharacteristicWrite?.Invoke(this, new EventArgs());
            IsReadyToSendWriteWithoutResponse?.Invoke(this, new EventArgs());
        };
        CharacteristicsDiscovered = true;
        GattReady = true;
        DeviceReady?.Invoke(this, new());
        MTU = 20;
    }

    public Task<nuint> RequestMTU(int size)
    {
        _GetMtuTask = new TaskCompletionSource<nuint>();
        _GattCallback.MtuChanged += GattCallback_MTUChanged;
        _Gatt.RequestMtu(size);
        return _GetMtuTask.Task;
    }

    private void GattCallback_MTUChanged(object? sender, EventDataArgs<nuint> e)
    {
        MTU = e.Data;
        _GattCallback.MtuChanged -= GattCallback_MTUChanged;
        _GetMtuTask?.TrySetResult(e.Data);
    }

    public void Write(string serviceID, string characteristicID, byte[] val, bool withResponse = true)
    {
        var ch = GetCharacteristic(serviceID, characteristicID);

        if (ch == null)
        {
            Debug.WriteLine("ConnectedDevice->Write - Characteristic Not Found.");
            return;
        }

        _WriteInProgress = true;

        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var writeType = withResponse ? GattWriteType.Default : GattWriteType.NoResponse;
            _Gatt.WriteCharacteristic(ch, val, (int)writeType);
        }
        else
        {
            ch.SetValue(val);
            _Gatt.WriteCharacteristic(ch);
        }
    }

    public void Read(string serviceID, string characteristicID, Action<KeyValuePair<string, byte[]?>> action, bool notify)
    {
        var ch = GetCharacteristic(serviceID, characteristicID);
        if (ch?.Descriptors is null) return;
        var d = ch.Descriptors[0];

        if (notify)
        {
            _GattCallback.CharacteristicChanged += (s, e) => action.Invoke(new KeyValuePair<string, byte[]?>(_Device.Address ?? "", e.Data));
            _Gatt.SetCharacteristicNotification(ch, true);

            if (BluetoothGattDescriptor.EnableNotificationValue is null) return;

            if (!OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                d.SetValue(BluetoothGattDescriptor.EnableNotificationValue.ToArray());
                _Gatt.WriteDescriptor(d); //descriptor write operation successfully started?
            }
            else
            {
                _Gatt.WriteDescriptor(d, BluetoothGattDescriptor.EnableNotificationValue.ToArray()); //descriptor write operation successfully started?
            }
        }
        else
        {
            _GattCallback.CharacteristicRead += (s, e) => action.Invoke(new KeyValuePair<string, byte[]?>(_Device.Address ?? "", e.Data));
            _Gatt.ReadDescriptor(d);
        }
    }

    public void Dispose()
    {
        if (_Gatt.Services is null) return;
        foreach (var service in _Gatt.Services)
        {
            if (service.Characteristics != null)
                foreach (var characteristic in service.Characteristics)
                    characteristic.Dispose();
            service.Dispose();
        }
    }

    public void StopNotifying(string serviceID, string characteristicID)
    {
        var ch = GetCharacteristic(serviceID, characteristicID);
        if (ch?.Descriptors is null) return;
        var d = ch.Descriptors[0];

        _Gatt.SetCharacteristicNotification(ch, false);
        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            if (BluetoothGattDescriptor.DisableNotificationValue is null) return;
            d.SetValue(BluetoothGattDescriptor.DisableNotificationValue.ToArray());
            _Gatt.WriteDescriptor(d);
        }
    }

    BluetoothGattCharacteristic? GetCharacteristic(string serviceID, string characteristicID)
    {
        var serviceUuid = Java.Util.UUID.FromString(serviceID);
        var charUuid = Java.Util.UUID.FromString(characteristicID);
        var svc = _Gatt.GetService(serviceUuid);
        return svc?.GetCharacteristic(charUuid) ?? null;
    }


    public IEnumerable<BLEService> GetServices()
    {
        var results = new List<BLEService>();
        var services = _Gatt.Services;

        if (services == null) return results;

        foreach (var svc in services)
        {
            var chList = new List<BLECharacteristic>();
            if (svc.Characteristics != null)
                foreach (var ch in svc.Characteristics)
                    chList.Add(new(ch.InstanceId.ToString().ToUpper(), ch.Properties.ToString()));
            results.Add(new(svc.InstanceId.ToString().ToUpper(), chList));
        }
        return results;
    }

    public bool CanSendWriteWithoutResponse() => !_WriteInProgress;
    public bool CanSendWrite() => !_WriteInProgress;
    public bool HasService(string serviceID) => _Gatt.Services?.FirstOrDefault(x => x.InstanceId.ToString().ToUpper() == serviceID.ToUpper()) != null;
    public bool HasCharacteristic(string serviceID, string characteristicID) => GetCharacteristic(serviceID, characteristicID) != null;

    public bool GattReady { get; private set; }
    public bool CharacteristicsDiscovered { get; private set; }
    public string Address { get; private set; }

    public nuint MTU { get; private set; }
    public event EventHandler<EventDataArgs<string[]>>? ServicesDiscovered = delegate { };
    public event EventHandler? IsReadyToSendWriteWithoutResponse;
    public event EventHandler? CharacteristicWrite;
    public event EventHandler? DeviceReady;

    readonly GattCallback _GattCallback;
    readonly BluetoothGatt _Gatt;
    readonly BluetoothDevice _Device;

    private bool _WriteInProgress = false;
    private TaskCompletionSource<nuint>? _GetMtuTask;
}