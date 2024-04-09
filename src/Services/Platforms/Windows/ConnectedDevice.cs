﻿using System;
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


    private void Gatt_MaxPduSizeChanged(object? sender, object e) =>
        MTU = (_Gatt is null || _Device.ConnectionStatus == BluetoothConnectionStatus.Disconnected) ? 20 : (uint)_Gatt.MaxPduSize;

    public async void Write(string serviceID, string characteristicID, byte[] val, bool withResponse = true)
    {
        var ch = GetCharacteristic(serviceID, characteristicID);

        if (ch is null) return;
        _WriteInProgress = true;
        var writeType = withResponse ? GattWriteOption.WriteWithResponse : GattWriteOption.WriteWithoutResponse;
        await ch.WriteValueAsync(CryptographicBuffer.CreateFromByteArray(val), writeType);
    }

    public async void Read(string serviceID, string characteristicID, Action<KeyValuePair<string, byte[]?>> action, bool notify)
    {
        try
        {
            _ReadAction = action;
            var deviceId = BLEUtils.ParseDeviceId(_Device.BluetoothAddress).ToString();
            var ch = GetCharacteristic(serviceID, characteristicID);

            if (ch is null) return;

            if (notify)
            {
                ch.ValueChanged += ValueChanged;
                var result = await ch.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
            }
            else
            {
                var readResult = await ch.ReadValueAsync(BluetoothCacheMode.Uncached);
                _ReadAction.Invoke(new KeyValuePair<string, byte[]?>(_Device.DeviceId, readResult.Value?.ToArray()));
            }
        } catch (Exception ex)
        {

        }
    }

    private void ValueChanged(object sender, GattValueChangedEventArgs e)
    {
        var deviceId = BLEUtils.ParseDeviceId(_Device.BluetoothAddress).ToString();
        _ReadAction?.Invoke(new KeyValuePair<string, byte[]?>(deviceId, e.CharacteristicValue?.ToArray()));
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

    public IEnumerable<BLEService> GetServices()
    {
        if (_Services is null) return null;
        return _Services.Select(s => new BLEService(s.Key.Uuid.ToString().ToUpper(), s.Value.Select(c => new BLECharacteristic(c.Uuid.ToString().ToUpper(), c.UserDescription))));
    }

    private KeyValuePair<GattDeviceService, IEnumerable<GattCharacteristic>>? GetService(string uuid) 
    {
        if (_Services is null) return null;
        return _Services.FirstOrDefault(s => s.Key.Uuid.ToString().Equals(uuid, StringComparison.CurrentCultureIgnoreCase));
    }
    
    private GattCharacteristic GetCharacteristic(string serviceID, string uuid)
    {
        var service = GetService(serviceID);
        if (service is null) return null;
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
    public event EventHandler<EventDataArgs<Tuple<string, string, byte[]>>>? CharacteristicChanged;
    public event EventHandler? DeviceReady;

    readonly GattSession _Gatt;
    readonly BluetoothLEDevice _Device;
    private bool _WriteInProgress = false;
    private Action<KeyValuePair<string, byte[]>> _ReadAction;
}