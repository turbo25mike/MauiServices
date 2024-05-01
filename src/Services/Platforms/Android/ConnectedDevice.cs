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

    public Task<IBLERequest> Write(IBLERequest request)
    {
        var id = Guid.NewGuid();
        Debug.WriteLine($"ConnectedDevice->Write({id})");

        var tcs = new TaskCompletionSource<IBLERequest>();
        var ct = new CancellationTokenSource(4000);

        EventHandler<CharacteristicWriteEventArgs>? callback = null;
        callback =
        (s, e) =>
        {
            Debug.WriteLine($"ConnectedDevice->Write({id}) - Written Response - s: {e.Characteristic.Service.Uuid} c: {e.Characteristic.Uuid}");
            if (request.ServiceID.Equals(e.Characteristic.Service.Uuid.ToString(), StringComparison.CurrentCultureIgnoreCase) && request.CharacteristicID.Equals(e.Characteristic.Uuid.ToString(), StringComparison.CurrentCultureIgnoreCase))
            {
                ct.Dispose();
                _GattCallback.CharacteristicWrite -= callback;
                request.SetResponse(new[] { (byte)e.Status });
                Debug.WriteLine($"ConnectedDevice->Write({id}) Response status: {e.Status}");
                if (e.Status == GattStatus.Success)
                {
                    var setSuccess = tcs.TrySetResult(request);
                    if(!setSuccess)
                        Debug.WriteLine($"ConnectedDevice->Write({id}) Response - Set Result Failed");
                }
                else
                    tcs.TrySetException(new ArgumentException($"Write({id}) Response - Failed: {e.Status}"));
            }
            else
            {
                Debug.WriteLine($"ConnectedDevice->Write({id}) Response: Service or characteristic did not match. ");
            }
        };

        ct.Token.Register(() =>
        {
            Debug.WriteLine($"ConnectedDevice->Write({id}): Timed out");
            _GattCallback.CharacteristicWrite -= callback;
            tcs.TrySetCanceled();
        }, useSynchronizationContext: false);
        
        var ch = GetCharacteristic(request.ServiceID, request.CharacteristicID);

        if (ch == null)
            return Task.FromException<IBLERequest>(new ArgumentException("Characteristic Not Found."));

        _GattCallback.CharacteristicWrite += callback;

        Debug.WriteLine($"ConnectedDevice->Write({id}) - Request - s: {request.ServiceID} c: {request.CharacteristicID}");

        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var attempts = 0;
            var maxAttempts = 5;
            while (attempts < maxAttempts)
            {
                attempts++;
                var writeType = request.WithResponse ? GattWriteType.Default : GattWriteType.NoResponse;
                var writeResult = _Gatt.WriteCharacteristic(ch, request.Data, (int)writeType);
                if (writeResult == 0)
                {
                    Debug.WriteLine($"ConnectedDevice->Write({id}) - WriteCharacteristic Success");
                    break;
                }
                else
                {
                    Debug.WriteLine($"ConnectedDevice->Write({id}) - WriteCharacteristic Failed - Error code: {writeResult} - attempt: {attempts}");

                    if (writeResult == 201) //Gatt is busy
                    {
                        Task.Delay(50).Wait();
                        Debug.WriteLine($"ConnectedDevice->Write({id}) WriteCharacteristic Failed Delay - {DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss.fff tt")}");
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
        else
        {
            ch.SetValue(request.Data);
            var writeSuccess = _Gatt.WriteCharacteristic(ch);
            if(!writeSuccess)
                Debug.WriteLine($"ConnectedDevice->Write({id}) - WriteCharacteristic Failed");
        }

        Debug.WriteLine($"ConnectedDevice->Write({id}) - Function Finished - Awaiting response");
        return tcs.Task;
    }

    public Task<IBLERequest> Read(IBLERequest request)
    {
        var tcs = new TaskCompletionSource<IBLERequest>();
        var ct = new CancellationTokenSource(4000);

        EventHandler<CharacteristicEventArgs>? callback = null;
        callback =
        (s, e) =>
        {
            Debug.WriteLine($"ConnectedDevice->Read - Response");
            if (request.ServiceID.Equals(e.Characteristic.Service.Uuid.ToString(), StringComparison.CurrentCultureIgnoreCase) && request.CharacteristicID.Equals(e.Characteristic.Uuid.ToString(), StringComparison.CurrentCultureIgnoreCase))
            {
                ct.Dispose();
                _GattCallback.CharacteristicRead -= callback;
                request.SetResponse(e.Data);
                tcs.TrySetResult(request);
            }
        };

        ct.Token.Register(() =>
        {
            _GattCallback.CharacteristicRead -= callback;
            tcs.TrySetCanceled();
        }, useSynchronizationContext: false);

        var ch = GetCharacteristic(request.ServiceID, request.CharacteristicID);
        if (ch?.Descriptors is null)
        {
            request.SetError("Descriptor is missing");
            return Task.FromResult(request);
        }

        _GattCallback.CharacteristicRead += callback;
        _Gatt.ReadDescriptor(ch.Descriptors[0]);
        return tcs.Task;
    }

    public void Dispose()
    {
        _GattCallback.CharacteristicChanged -= _GattCallback_CharacteristicChanged;
        _GattCallback.MtuChanged -= GattCallback_MTUChanged;
                
        if (_Gatt.Services is null) return;
        foreach (var service in _Gatt.Services)
        {
            if (service.Characteristics != null)
                foreach (var characteristic in service.Characteristics)
                    characteristic.Dispose();
            service.Dispose();
        }
    }

    public async Task StartNotifying(string serviceID, string characteristicID)
    {
        _GattCallback.CharacteristicChanged -= _GattCallback_CharacteristicChanged;
        _GattCallback.CharacteristicChanged += _GattCallback_CharacteristicChanged;
        var success = false;
        var ch = GetCharacteristic(serviceID, characteristicID);
        if (ch?.Descriptors is null) return;
        var d = ch.Descriptors[0];

        _Gatt.SetCharacteristicNotification(ch, true);

        if (BluetoothGattDescriptor.EnableNotificationValue is null) return;

        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            d.SetValue(BluetoothGattDescriptor.EnableNotificationValue.ToArray());
            success = _Gatt.WriteDescriptor(d); //descriptor write operation successfully started?
        }
        else
            success = _Gatt.WriteDescriptor(d, BluetoothGattDescriptor.EnableNotificationValue.ToArray()) == 0; //descriptor write operation successfully started?
        await Task.Delay(250);
    }

    private void _GattCallback_CharacteristicChanged(object? sender, CharacteristicEventArgs e) //notification response
    {
        CharacteristicChanged?.Invoke(this, new(new(e.Characteristic.Service.Uuid.ToString(), e.Characteristic.Uuid.ToString(), e.Data)));
    }

    public void StopNotifying(string serviceID, string characteristicID)
    {
        _GattCallback.CharacteristicChanged -= _GattCallback_CharacteristicChanged;

        var ch = GetCharacteristic(serviceID, characteristicID);
        if (ch?.Descriptors is null) return;
        var d = ch.Descriptors[0];

        var disabled = _Gatt.SetCharacteristicNotification(ch, false);
        if (!disabled && !OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            if (BluetoothGattDescriptor.DisableNotificationValue is null) return;
            d.SetValue(BluetoothGattDescriptor.DisableNotificationValue.ToArray());
            _Gatt.WriteDescriptor(d);
            disabled = true;
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

    public bool HasService(string serviceID) => _Gatt.Services?.FirstOrDefault(x => x.InstanceId.ToString().ToUpper() == serviceID.ToUpper()) != null;
    public bool HasCharacteristic(string serviceID, string characteristicID) => GetCharacteristic(serviceID, characteristicID) != null;

    public bool GattReady { get; private set; }
    public bool CharacteristicsDiscovered { get; private set; }
    public string Address { get; private set; }

    public nuint MTU { get; private set; }
    public event EventHandler<EventDataArgs<string[]>>? ServicesDiscovered = delegate { };
    public event EventHandler? DeviceReady;
    public event EventHandler<EventDataArgs<Tuple<string, string, byte[]>>>? CharacteristicChanged;

    readonly GattCallback _GattCallback;
    readonly BluetoothGatt _Gatt;
    readonly BluetoothDevice _Device;

    private TaskCompletionSource<nuint>? _GetMtuTask;
}