﻿using System.Diagnostics;
using Windows.Devices.Bluetooth.Advertisement;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Radios;
using System.Net;

namespace Turbo.Maui.Services.Platforms;

public partial class BLEAdapter : IBluetoothAdapter
{
    public BLEAdapter()
    {
        _Adapter = BluetoothAdapter.GetDefaultAsync().AsTask().Result;
        _Radio = _Adapter.GetRadioAsync().AsTask().Result;
        _Radio.StateChanged += (s, e) => BluetoothStateChanged?.Invoke(this, new());
    }

    private BluetoothAdapter _Adapter;
    private readonly Radio _Radio;
    private BluetoothLEAdvertisementWatcher? _BleWatcher;
    public bool IsPoweredOn => _Radio.State == RadioState.On;
    public bool CanAccess => _Radio.State != RadioState.Disabled;
    public bool IsAdvertising => false;

    public event EventHandler<EventArgs> BluetoothStateChanged;
    public bool IsScanning { get; private set; }


    public void StartAdvertising(BLEAdvertisingManager manager) { }
    public void StopAdvertising() { }
    public void Notify(string serviceID, string characteristicID, string value) { }

    public bool StartScanningForDevices(string[]? uuids = null, int? manufacturerID = null)
    {
        if (IsScanning) return true;
        if (manufacturerID != null)
            _ManufacturerIDFilter = (ushort)manufacturerID;
        AddPeripherals(uuids);

        _BleWatcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active, AllowExtendedAdvertisements = true };

        Debug.WriteLine("Starting a scan for devices.");
        //this appears to not be working as a filter so we will filter when devices are found.
        //if (_PeripheralUUIDs.Any())
        //{
        //    //adds filter to native scanner if serviceUuids are specified
        //    foreach (var uuid in _PeripheralUUIDs)
        //        _BleWatcher.AdvertisementFilter.Advertisement.ServiceUuids.Add(uuid);

        //    Debug.WriteLine($"ScanFilters: {string.Join(", ", _PeripheralUUIDs)}");
        //}
        _BleWatcher.Received += OnScanResult;
        _BleWatcher.Start();
        IsScanning = true;
        return true;
    }

    public void StopScanningForDevices()
    {
        if (_BleWatcher != null)
        {
            Debug.WriteLine("Stopping the scan for devices");
            _BleWatcher.Received -= OnScanResult;
            _BleWatcher.Stop();
            _BleWatcher = null;
        }

        IsScanning = false;
    }

    public async void ConnectTo(string address)
    {
        Debug.WriteLine($"ConnectTo: {address}");
        DeviceConnectionStatus?.Invoke(this, new(BLEDeviceStatus.Connecting));
        StopScanningForDevices();
        var d = _DevicesFound[Guid.Parse(address)];
        try
        {
            _GattSession = await GattSession.FromDeviceIdAsync(d.BluetoothDeviceId);
            _GattSession.MaintainConnection = true;
            _GattSession.SessionStatusChanged += GattSession_SessionStatusChanged;
            _GattSession.MaxPduSizeChanged += GattSession_MaxPduSizeChanged;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("WARNING ConnectInternal failed: {0}", ex.Message);
            DisposeGattSession();
        }

        if (_GattSession == null)
        {
            // use DisconnectDeviceNative to clean up resources otherwise windows won't disconnect the device
            // after a subsequent successful connection (#528, #536, #423)
            DisconnectDevice();

            // fire a connection failed event
            DeviceConnectionStatus?.Invoke(this, new(BLEDeviceStatus.Disconnected));
        }
        else
        {
            if (d.ConnectionStatus == BluetoothConnectionStatus.Connected) GetServices(d);
            else
                d.ConnectionStatusChanged += ConnectionStatusChanged; 
            
        }
    }

    private void ConnectionStatusChanged(BluetoothLEDevice sender, object args)
    {
        Debug.WriteLine("ConnectionStatusChanged: " + sender.ConnectionStatus);
        if (sender.ConnectionStatus == BluetoothConnectionStatus.Connected) GetServices(sender);
        if(sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected) DisconnectDevice();
    }

    private async void GetServices(BluetoothLEDevice d)
    {
        //Let's fetch the services/characteristics
        var services = new Dictionary<GattDeviceService, IEnumerable<GattCharacteristic>>();
        var result = await d.GetGattServicesAsync(BluetoothCacheMode.Cached);
        foreach (var s in result.Services)
        {
            var ch = await s.GetCharacteristicsAsync(BluetoothCacheMode.Cached);
            services.Add(s, ch.Characteristics);
        }

        ConnectedDevice = new ConnectedDevice(d, _GattSession, services);

        DeviceConnectionStatus?.Invoke(this, new(BLEDeviceStatus.Connected));
    }

    private void DisposeGattSession()
    {
        if (_GattSession != null)
        {
            _GattSession.MaintainConnection = false;
            _GattSession.MaxPduSizeChanged -= GattSession_MaxPduSizeChanged;
            _GattSession.SessionStatusChanged -= GattSession_SessionStatusChanged;
            _GattSession.Dispose();
            _GattSession = null;
        }
    }

    private void GattSession_SessionStatusChanged(GattSession sender, GattSessionStatusChangedEventArgs args)
    {
        Debug.WriteLine("GattSession_SessionStatusChanged: " + args.Status);
    }

    private void GattSession_MaxPduSizeChanged(GattSession sender, object args)
    {
        Debug.WriteLine("GattSession_MaxPduSizeChanged: {0}", sender.MaxPduSize);
    }

    public void DisconnectDevice()
    {
        if (ConnectedDevice == null) return;
        // Windows doesn't support disconnecting, so currently just dispose of the device
        ConnectedDevice.Dispose();
        DisposeGattSession();
        ConnectedDevice = null;
        DeviceConnectionStatus?.Invoke(this, new(BLEDeviceStatus.Disconnected));
    }

    /// <summary>
    /// Handler for devices found when duplicates are not allowed
    /// </summary>
    /// <param name="watcher">The bluetooth advertisement watcher currently being used</param>
    /// <param name="btAdv">The advertisement recieved by the watcher</param>
    private void OnScanResult(BluetoothLEAdvertisementWatcher watcher, BluetoothLEAdvertisementReceivedEventArgs btAdv)
    {
        var deviceId = BLEUtils.ParseDeviceId(btAdv.BluetoothAddress);

        //if (_PeripheralUUIDs.Any())
        //{
        //    bool containsAny = false;
        //    foreach (var uuid in _PeripheralUUIDs)
        //        if (btAdv.Advertisement.ServiceUuids.Contains(uuid))
        //        {
        //            containsAny = true;
        //            break;
        //        }
        //    if (!containsAny) return;
        //}

        //if (_ManufacturerIDFilter.HasValue)
        //{
        //    var mfgData = btAdv.Advertisement.GetManufacturerDataByCompanyId(_ManufacturerIDFilter.Value);  //this only allows us to add our company devices. 
        //    if (mfgData.Count < 1) return;
        //}

        var bluetoothLeDevice = BluetoothLEDevice.FromBluetoothAddressAsync(btAdv.BluetoothAddress).AsTask().Result;
        if (bluetoothLeDevice is null) //make sure advertisement bluetooth address actually returns a device
            return;


        if (_DevicesFound.ContainsKey(deviceId))
            _DevicesFound[deviceId] = bluetoothLeDevice;
        else
            _DevicesFound.Add(deviceId, bluetoothLeDevice);

        var advList = btAdv.Advertisement.DataSections;
        Packet packet = new Packet
        {
            RSSI = btAdv.RawSignalStrengthInDBm,
            ID = deviceId.ToString(),
            Name = btAdv.Advertisement.LocalName,

        };

        //var adData = System.Text.Encoding.Default.GetString(advList[0].Data.ToArray());

        var serviceData = advList.FirstOrDefault((d) => d.DataType == 0x16);
        if (serviceData != null)
            packet.ServiceData = serviceData.Data.ToArray();

        var manufacturerData = advList.FirstOrDefault((d) => d.DataType == 0xFF);
        if (manufacturerData != null)
            packet.ManufacturerData = manufacturerData.Data.ToArray();


        DeviceDiscovered?.Invoke(this, new(packet));
    }   

    Dictionary<Guid, BluetoothLEDevice> _DevicesFound = new();

    public event EventHandler<EventDataArgs<BLEDeviceStatus>> DeviceConnectionStatus = delegate { };

    private GattSession? _GattSession;

    public event EventHandler<EventDataArgs<Packet>> DeviceDiscovered = delegate { };
    public IConnectedDevice? ConnectedDevice { get; private set; }

    private List<Guid> _PeripheralUUIDs = new();
    private ushort? _ManufacturerIDFilter;

    private void AddPeripherals(string[]? uuids)
    {
        if (uuids != null)
        {
            foreach (var uuid in uuids)
            {
                if (Guid.TryParse(uuid, out var g))
                {
                    if (!_PeripheralUUIDs.Contains(g))
                        _PeripheralUUIDs.Add(g);
                }
            }
        }
    }
}