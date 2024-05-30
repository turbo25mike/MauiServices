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

        _Manager = new CBPeripheralManager();
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
            DisposeConnectedDevice();
        };
    }

    private BLEAdvertisingManager? _AdvertisingManager;

    public void StartAdvertising(BLEAdvertisingManager manager)
    {
        StopAdvertising();
        _AdvertisingManager = manager;
        _Manager.WriteRequestsReceived += Manager_WriteRequestsReceived;
        _Manager.ReadRequestReceived += Manager_ReadRequestReceived;
        _Manager.CharacteristicSubscribed += Manager_CharacteristicSubscribed;
        _Manager.CharacteristicUnsubscribed += Manager_CharacteristicUnsubscribed;

        _NativeServices.AddRange(BuildServices(manager.Services));
        if (_NativeServices.Count == 0) return;
        var nativeServiceUUIDs = _NativeServices.Select(s => s.UUID);


        foreach (var service in _NativeServices)
            _Manager.AddService(service);

        _Manager.StartAdvertising(new StartAdvertisingOptions() { LocalName = manager.LocalName, ServicesUUID = nativeServiceUUIDs.ToArray() });
    }

    private readonly List<CBMutableService> _NativeServices = new();

    private CBMutableService[] BuildServices(BLEService[] services)
    {
        var sysServices = new List<CBMutableService>();

        foreach (var service in services)
        {
            var s = new CBMutableService(CBUUID.FromString(service.UUID), service.IsPrimary);
            var chList = new List<CBMutableCharacteristic>();
            foreach (var characteristic in service.Characteristics)
            {
                var ch = new CBMutableCharacteristic(CBUUID.FromString(characteristic.UUID), ConvertProperties(characteristic.Properties), characteristic.Value is null ? null : new NSData(characteristic.Value, NSDataBase64DecodingOptions.IgnoreUnknownCharacters), ConvertPermission(characteristic.Permissions));
                chList.Add(ch);
            }
            s.Characteristics = chList.ToArray();
            sysServices.Add(s);
        }
        return sysServices.ToArray();
    }

    private CBCharacteristicProperties ConvertProperties(string properties) =>
        properties switch
        {
            "Read" => CBCharacteristicProperties.Read,
            "Read, Write" => CBCharacteristicProperties.Read | CBCharacteristicProperties.Write,
            "Write" => CBCharacteristicProperties.Write,
            "WriteWithoutResponse" => CBCharacteristicProperties.WriteWithoutResponse,
            "Indicate" => CBCharacteristicProperties.Indicate,
            "Notify" => CBCharacteristicProperties.Notify,
            _ => CBCharacteristicProperties.Read
        };

    private CBAttributePermissions ConvertPermission(BLEPermissions permissions) =>
        permissions switch
        {
            BLEPermissions.Readable => CBAttributePermissions.Readable,
            BLEPermissions.Writeable => CBAttributePermissions.Writeable,
            BLEPermissions.ReadEncryptionRequired => CBAttributePermissions.ReadEncryptionRequired,
            BLEPermissions.WriteEncryptionRequired => CBAttributePermissions.WriteEncryptionRequired,
            _ => CBAttributePermissions.Readable
        };

    private readonly Dictionary<CBCharacteristic, List<CBCentral>> Subscribers = new();

    private void Manager_CharacteristicUnsubscribed(object? sender, CBPeripheralManagerSubscriptionEventArgs e)
    {
        if (_AdvertisingManager is null) throw new ArgumentNullException("AdvertisingManager must be set");
        if (e.Characteristic.Service is null) return;
        _AdvertisingManager.Unsubscribed(e.Central.Identifier.ToString(), e.Characteristic.Service.UUID.ToString(), e.Characteristic.UUID.ToString());

        if (Subscribers.TryFirstOrDefault((ch) => ch.Key.UUID == e.Characteristic.UUID, out var found))
            found.Value.Remove(e.Central);
    }

    private void Manager_CharacteristicSubscribed(object? sender, CBPeripheralManagerSubscriptionEventArgs e)
    {
        if (_AdvertisingManager is null) throw new ArgumentNullException("AdvertisingManager must be set");
        if (e.Characteristic.Service is null) return;

        _AdvertisingManager.Subscribed(e.Central.Identifier.ToString(), e.Characteristic.Service.UUID.ToString(), e.Characteristic.UUID.ToString());

        if(Subscribers.TryFirstOrDefault((ch) => ch.Key.UUID == e.Characteristic.UUID, out var found))
            found.Value.Add(e.Central);
        else
            Subscribers.Add(e.Characteristic, new() { e.Central });

        //NSData * updatedValue = // fetch the characteristic's new value
        //BOOL didSendValue = [myPeripheralManager updateValue: updatedValue
        //forCharacteristic: characteristic onSubscribedCentrals: nil];
    }    

    private void Manager_WriteRequestsReceived(object? sender, CBATTRequestsEventArgs e)
    {
        if (_AdvertisingManager is null) throw new ArgumentNullException("AdvertisingManager must be set");
        
        foreach (var request in e.Requests)
        {
            if (request.Characteristic.Service is null) continue;
            _AdvertisingManager.WriteRequested(request.Central.Identifier.ToString(), request.Characteristic.Service.UUID.ToString(), request.Characteristic.UUID.ToString(), request.Value?.ToString() ?? "");
        }
    }

    private void Manager_ReadRequestReceived(object? sender, CBATTRequestEventArgs e)
    {
        if (_AdvertisingManager is null) throw new ArgumentNullException("AdvertisingManager must be set");

        if (e.Request.Characteristic.Service is null)
        {
            _Manager.RespondToRequest(e.Request, CBATTError.InvalidHandle);
            return;
        }
        
        var response = _AdvertisingManager.ReadRequested(e.Request.Central.Identifier.ToString(), e.Request.Characteristic.Service.UUID.ToString(), e.Request.Characteristic.UUID.ToString());
        e.Request.Value = response is null ? null :  NSData.FromString(response, NSStringEncoding.ASCIIStringEncoding);
        _Manager.RespondToRequest(e.Request, CBATTError.Success);
    }

    public void Notify(string serviceID, string characteristicID, string value)
    {
        var service = _NativeServices.FirstOrDefault(s => string.Equals(s.UUID.ToString(), serviceID, StringComparison.CurrentCultureIgnoreCase));
        if (service is null) return;
        var characteristic = service.Characteristics?.FirstOrDefault(c => string.Equals(c.UUID.ToString(), characteristicID, StringComparison.CurrentCultureIgnoreCase));
        if (characteristic is null) return;
   
        var didSend = _Manager.UpdateValue(NSData.FromString(value), (CBMutableCharacteristic)characteristic, null);
                
    }

    public void StopAdvertising()
    {
        //if (_Manager.Advertising)
        //    StopAdvertising();
    }

    public bool StartScanningForDevices(string[]? uuids = null, int? manufacturerID = null)
    {
        if (IsScanning) return true;

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
            return true;
        }
        else
        {
            IsScanning = false;
            Debug.WriteLine("BLEAdapter: Bluetooth is Disabled.");
            return false;
        }
    }

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
        if (_ConnectedPeripheral is null) return;
        _Adapter.CancelPeripheralConnection(_ConnectedPeripheral);
        DisposeConnectedDevice();
    }

    private void _Adapter_UpdatedState(object? sender, EventArgs e)
    {
        BluetoothStateChanged?.Invoke(this, new EventArgs());
        Debug.WriteLine($"BLEAdapter State Updated: {_Adapter.State}");
        if (_Adapter.State == CBManagerState.PoweredOn && _IsRunning)
            StartScanningForDevices();
        else if (_Adapter.State == CBManagerState.PoweredOff)
        {
            _DevicesFound.Clear();
            var wasRunning = _IsRunning;
            StopScanningForDevices();
            _IsRunning = wasRunning;
            DisposeConnectedDevice();
        }
    }

    private void DisposeConnectedDevice()
    {
        Debug.WriteLine("BLE Adapter: Disposing Connected Device");

        if (ConnectedDevice != null)
        {
            ConnectedDevice.Dispose();
            ConnectedDevice = null;
        }

        if (_ConnectedPeripheral != null)
        {
            _ConnectedPeripheral?.Dispose();
            _ConnectedPeripheral = null;
        }

        DeviceConnectionStatus?.Invoke(this, new(BLEDeviceStatus.Disconnected));
    }

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

    private void OnScanResult(object? sender, CBDiscoveredPeripheralEventArgs result)
    {
        try
        {
            if (result.Peripheral == null) return;

            var ad = result.AdvertisementData;
            if (ad.Values == null) return;

            var name = result.Peripheral.Name;
            if (ad.ContainsKey(CBAdvertisement.DataLocalNameKey))
            {
                // iOS caches the peripheral name, so it can become stale (if changing)
                // keep track of the local name key manually
                name = ((NSString)ad.ValueForKey(CBAdvertisement.DataLocalNameKey)).ToString();
            }

            Packet packet = new Packet
            {
                RSSI = result.RSSI.Int32Value,
                ID = result.Peripheral.Identifier.ToString(),
                Name = name ?? ""
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

    public CBManagerState GetCurrentState() => _Adapter.State;
    public bool IsPoweredOn => _Adapter.State == CBManagerState.PoweredOn;
    public bool CanAccess => _Adapter.State != CBManagerState.Unauthorized;
    public bool IsAdvertising => _Manager.Advertising;
    public IConnectedDevice? ConnectedDevice { get; private set; }
    public event EventHandler<EventDataArgs<BLEDeviceStatus>> DeviceConnectionStatus = delegate { };
    public event EventHandler<EventDataArgs<Packet>> DeviceDiscovered = delegate { };
    public event EventHandler<EventArgs> BluetoothDisabled = delegate { };
    public event EventHandler<EventArgs>? BluetoothStateChanged;
    public bool IsScanning { get; private set; }

    private List<CBUUID> _PeripheralUUIDs = new();
    bool _IsRunning; //this enables scanning to restart when the bluetooth gets turned off/on outside of the app.
    Dictionary<string, CBPeripheral> _DevicesFound = new();
    CBPeripheral? _ConnectedPeripheral;
    private readonly CBPeripheralManager _Manager;
    readonly CBCentralManager _Adapter;
}