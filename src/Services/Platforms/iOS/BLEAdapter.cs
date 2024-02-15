using System.Diagnostics;
using CoreBluetooth;
using CoreFoundation;
using Foundation;

namespace Turbo.Maui.Services.Platforms;

public partial class BLEAdapter : IBluetoothAdapter
{
    public BLEAdapter()
    {
        Debug.WriteLine("BLEAdapter Init()");
        _Adapter = new CBCentralManager(DispatchQueue.MainQueue);

        _Adapter.UpdatedState += _Adapter_UpdatedState;
        _Adapter.DiscoveredPeripheral += OnScanResult;
        _Adapter.ConnectedPeripheral += (s, e) =>
        {
            Debug.WriteLine("BLE Connected");
            ConnectedDevice = new ConnectedDevice(e.Peripheral);
            _ConnectedPeripheral = e.Peripheral;
            DeviceConnectionStatus?.Invoke(this, new(BLEDeviceStatus.Connected));
        };

        _Adapter.DisconnectedPeripheral += (s, e) =>
        {

            Debug.WriteLine("BLE Disconnected");
            _ConnectedPeripheral?.Dispose();
            _ConnectedPeripheral = null;
            ConnectedDevice = null;
            DeviceConnectionStatus?.Invoke(this, new(BLEDeviceStatus.Disconnected));
        };
    }

    void _Adapter_UpdatedState(object? sender, EventArgs e)
    {
        BluetoothStateChanged?.Invoke(this, new EventArgs());
        Debug.WriteLine($"BLEAdapter State Updated: {_Adapter.State}");
        if (_Adapter.State == CBManagerState.PoweredOn && _IsRunning)
            StartScanningForDevices();
        else if (_Adapter.State == CBManagerState.PoweredOff)
        {
            var wasRunning = _IsRunning;
            StopScanningForDevices();
            _IsRunning = wasRunning;
        }
    }

    public CBManagerState GetCurrentState() => _Adapter.State;

    private List<CBUUID> _PeripheralUUIDs = new();
    private void AddPeripherals(string[]? uuids)
    {
        if (uuids != null)
        {
            foreach (var uuid in uuids)
            {
                var cbuuid = CBUUID.FromString(uuid);

                if (!_PeripheralUUIDs.Contains(cbuuid))
                    _PeripheralUUIDs.Add(cbuuid);
            }
        }
    }

    public Task<bool> StartScanningForDevices(string[]? uuids = null, int? manufacturerID = null)
    {
        if (IsScanning) return Task.FromResult(true);

        AddPeripherals(uuids);

        _IsRunning = true;

        _DevicesFound = new Dictionary<string, CBPeripheral>();

        Debug.WriteLine("BLEAdapter State: " + _Adapter.State);

        if (_Adapter.State == CBManagerState.PoweredOn)
        {
            IsScanning = true;
#if __UNIFIED__
            var options = new PeripheralScanningOptions { AllowDuplicatesKey = true };


            _Adapter.ScanForPeripherals(_PeripheralUUIDs.ToArray(), options);
            //_Adapter.ScanForPeripherals(null, options); //Does not work in background
            Debug.WriteLine("BLEAdapter: Scanning");
#else
			    _central.ScanForPeripherals (serviceUuids:null);
#endif
            return Task.FromResult(true);
        }
        else
        {
            IsScanning = false;
            Debug.WriteLine("BLEAdapter: Bluetooth is Disabled.");
            return Task.FromResult(false);
        }
    }

    public bool IsPoweredOn => _Adapter.State == CBManagerState.PoweredOn;

    public bool CanAccess => _Adapter.State != CBManagerState.Unauthorized;

    public void StopScanningForDevices()
    {
        _IsRunning = false;
        if (!IsScanning) return;
        Debug.WriteLine("BLEAdapter: Scan stopped");
        IsScanning = false;
        _Adapter.StopScan();
    }

    public void ConnectTo(string address)
    {
        Debug.WriteLine($"BLEAdapter - Connecting to: {address}");
        var d = _DevicesFound[address];
        if (d == null)
        {
            Debug.WriteLine($"Device - {address} not found.");
            return;
        }

        DeviceConnectionStatus?.Invoke(this, new(BLEDeviceStatus.Connecting));

        StopScanningForDevices();
        _Adapter.ConnectPeripheral(d);
    }

    public void DisconnectDevice()
    {
        if (_ConnectedPeripheral == null) return;
        _Adapter.CancelPeripheralConnection(_ConnectedPeripheral);
    }

    void OnScanResult(object? sender, CBDiscoveredPeripheralEventArgs result)
    {
        try
        {
            if (result.Peripheral == null) return;

            var ad = result.AdvertisementData;
            if (ad.Values == null) return;

            var localname = ad.ValueForKey(CBAdvertisement.DataLocalNameKey); //localname may be different then cached name. see: https://developer.apple.com/forums/thread/72343

            Packet packet = new Packet
            {
                RSSI = result.RSSI.Int32Value,
                ID = result.Peripheral.Identifier.ToString(),
                Name = localname?.ToString()
            };

            var md = ad.ValueForKey(CBAdvertisement.DataManufacturerDataKey);
            if (md != null)
                packet.ManufacturerData = ((NSData)md).ToArray();

            var sd = ad.ValueForKey(CBAdvertisement.DataServiceDataKey);
            if (sd != null)
            {
                var serviceData = (NSDictionary)sd;
                if (serviceData.Values != null && serviceData.Values.Length > 0)
                    packet.ServiceData = ((NSData)serviceData.Values[0]).ToArray();
            }

            if (_DevicesFound.ContainsKey(result.Peripheral.Identifier.ToString()))
                _DevicesFound[result.Peripheral.Identifier.ToString()] = result.Peripheral;
            else
                _DevicesFound.Add(result.Peripheral.Identifier.ToString(), result.Peripheral);

            DeviceDiscovered?.Invoke(this, new(packet));
        }
        catch (Exception ex)
        {
            Debug.WriteLine("BLEAdapter: Exception: " + ex);
        }
    }

    public IConnectedDevice? ConnectedDevice { get; private set; }
    public event EventHandler<EventDataArgs<BLEDeviceStatus>> DeviceConnectionStatus = delegate { };
    public event EventHandler<EventDataArgs<Packet>> DeviceDiscovered = delegate { };
    public event EventHandler<EventArgs> BluetoothDisabled = delegate { };
    public event EventHandler<EventArgs> BluetoothStateChanged;
    public bool IsScanning { get; private set; }

    bool _IsRunning; //this enables scanning to restart when the bluetooth gets turned off/on outside of the app.
    Dictionary<string, CBPeripheral> _DevicesFound = new();
    CBPeripheral? _ConnectedPeripheral;
    readonly CBCentralManager _Adapter;
}