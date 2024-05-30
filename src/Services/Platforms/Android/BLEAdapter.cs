using System.Text;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.OS;
using Java.Util;
using ScanMode = Android.Bluetooth.LE.ScanMode;

namespace Turbo.Maui.Services.Platforms;

public partial class BLEAdapter : ScanCallback, IBluetoothAdapter
{
    public BLEAdapter()
    {
        var context = MauiApplication.Current;
        if (context.GetSystemService(Context.BluetoothService) is not BluetoothManager bluetoothManager) return;
        _BluetoothManager = bluetoothManager;
        _Adapter = _BluetoothManager.Adapter;
        _DevicesFound = new Dictionary<string, BluetoothDevice>();
    }

    #region Public Methods

    public void StartAdvertising(BLEAdvertisingManager manager)
    {
        StopAdvertising();

        _Manager = new GattServerCallback();

        _Server = _BluetoothManager.OpenGattServer(MauiApplication.Current, _Manager);
        _AdvertisingManager = manager;

        _Manager.CharacteristicWrite += Manager_CharacteristicWrite;
        _Manager.CharacteristicRead += Manager_CharacteristicRead;
        _Manager.NotificationSent += Manager_NotificationSent;
        //_Manager.CharacteristicSubscribed += Manager_CharacteristicSubscribed;
        //_Manager.CharacteristicUnsubscribed += Manager_CharacteristicUnsubscribed;

        foreach (var service in _AdvertisingManager.Services)
            _Server?.AddService(BuildService(service));
    }

    private void Manager_NotificationSent(object? sender, NotificationSentServerEventArgs e)
    {

    }

    private void Manager_CharacteristicRead(object? sender, CharacteristicReadServerEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void Manager_CharacteristicWrite(object? sender, CharacteristicWriteServerEventArgs e)
    {
        throw new NotImplementedException();
    }

    public void StopAdvertising()
    {
        if (_Server is null) return;
        _Server.Close();
        _Server.Dispose();
        _Server = null;
    }

    public void Notify(string serviceID, string characteristicID, string value)
    {
        if (_Server?.ConnectedDevices is null || _Server.Services is null) return;

        if(_Server.Services.TryFirstOrDefault((s) => s.Uuid == UUID.FromString(serviceID), out var service))
        {
            if (service?.Characteristics is null) return;

            if(service.Characteristics.TryFirstOrDefault((c) => c.Uuid == UUID.FromString(characteristicID), out var characteristic))
            {
                if (characteristic is null) return;
                foreach (var d in _Server.ConnectedDevices)
                {
                    if (OperatingSystem.IsAndroidVersionAtLeast(33))
                    {
                        _Server.NotifyCharacteristicChanged(d, characteristic, false, Encoding.ASCII.GetBytes(value));
                    }
                    else
                    {
                        _Server.NotifyCharacteristicChanged(d, characteristic, false);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Starts BLE scanning if it is not currently running.
    /// </summary>
    /// <param name="uuids">Service IDs for devices.  Null searches for all devices but doesn't work in the app goes into the background.</param>
    /// <param name="manufacturerID">Required in Android to gather manufacturer data.</param>
    /// <returns></returns>
    public bool StartScanningForDevices(string[]? uuids = null, int? manufacturerID = null)
    {
        try
        {
            _ManufacturerID = manufacturerID;

            if (_Adapter?.BluetoothLeScanner is null) return false;

            if (IsScanning) return true;

            _DevicesFound?.Clear();
            if (uuids != null)
                AddPeripherals(uuids);

            if (_Adapter.IsEnabled)
            {
                IsScanning = true;
                var scan = new ScanSettings.Builder();
                if (scan is null) return false;
                scan.SetScanMode(ScanMode.LowPower);
                var settings = scan.Build();
                if (settings is null) return false;
                List<ScanFilter> filters = new();
                foreach (UUID serviceUUID in _PeripheralUUIDs)
                {
                    var filter = new ScanFilter.Builder().SetServiceUuid(new ParcelUuid(serviceUUID));
                    if (filter is null) continue;
                    var scanFilter = filter.Build();
                    if (scanFilter != null)
                        filters.Add(scanFilter);
                }

                _Adapter.BluetoothLeScanner.StartScan(filters.Count > 0 ? filters : null, settings, this);
                _Adapter.StartDiscovery();
                System.Diagnostics.Debug.WriteLine("Scanning...");

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Scanning failed: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Stops BLE scanning if it is currently running.
    /// </summary>
    public void StopScanningForDevices()
    {
        if (!IsScanning || _Adapter?.BluetoothLeScanner is null)
            return;
        IsScanning = false;
        _Adapter.BluetoothLeScanner.StopScan(this);
        System.Diagnostics.Debug.WriteLine("Scanning Stopped");
    }

    public override void OnScanResult(ScanCallbackType callbackType, ScanResult? result)
    {
        try
        {
            if (_Adapter is null || !_Adapter.IsEnabled) return;

            System.Diagnostics.Debug.WriteLine("OnScanResult");
            if (result?.Device is null) return;
            if (_DevicesFound != null && result.Device.Address != null)
            {
                if (result.Device != null && _DevicesFound.ContainsKey(result.Device.Address))
                    _DevicesFound[result.Device.Address] = result.Device;
                else
                    _DevicesFound.Add(result.Device.Address, result.Device);
            }

            var sdData = new List<byte>();
            var mdData = new List<byte>();

            foreach (var uuid in _PeripheralUUIDs)
            {
                var sd = result.ScanRecord?.GetServiceData(ParcelUuid.FromString(uuid.ToString()));
                if (sd != null)
                {
                    sdData.AddRange(sd);
                    break;
                }
            }

            if (_ManufacturerID != null)
            {
                var md = result.ScanRecord?.GetManufacturerSpecificData(_ManufacturerID.Value);
                if (md != null)
                {
                    mdData.AddRange(new byte[2]); //holder for missing manufacturer id.  This is added in iOS so we must match
                    mdData.AddRange(md);
                }
            }

            var device = new Packet { Name = result.Device?.Name ?? "", ID = result.Device?.Address ?? "", TxPower = result.ScanRecord?.TxPowerLevel ?? 0, ServiceData = sdData.ToArray(), ManufacturerData = mdData.ToArray(), RSSI = result.Rssi };
            DeviceDiscovered?.Invoke(this, new(device));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Adapter: Exception: " + ex);
        }
    }

    public void ConnectTo(string address)
    {
        StopScanningForDevices();

        if (_DevicesFound is null || !_DevicesFound.ContainsKey(address)) return;
        if (ConnectedDevice != null) return;

        var thread = Dispatcher.GetForCurrentThread();
        if (thread is null) return;
        thread.Dispatch(() =>
        {
            var d = _DevicesFound[address];

            var callback = new GattCallback();
            callback.DeviceStatus += (s, e) =>
            {
                if (_ConnectedDeviceGatt is null) return;
                if (e.Data == BLEDeviceStatus.Connected)
                {
                    _ConnectedDeviceGatt.DiscoverServices();
                }
                else if (e.Data == BLEDeviceStatus.Disconnected)
                {
                    _ConnectedDeviceGatt.Disconnect();
                    _ConnectedDeviceGatt.Close();
                    _ConnectedDeviceGatt = null;
                    ConnectedDevice = null;
                    DeviceConnectionStatus?.Invoke(this, e);
                }
                else
                    DeviceConnectionStatus?.Invoke(this, e);
            };
            callback.ServicesDiscovered += (s, e) =>
            {
                if (_ConnectedDeviceGatt is null) throw new ArgumentNullException("ConnectedDevice");
                ConnectedDevice = new ConnectedDevice(address, d, _ConnectedDeviceGatt, callback);
                DeviceConnectionStatus?.Invoke(this, new(BLEDeviceStatus.Connected));
            };
            DeviceConnectionStatus?.Invoke(this, new(BLEDeviceStatus.Connecting));
            _ConnectedDeviceGatt = d.ConnectGatt(MauiApplication.Current, false, callback);
        });
    }

    public void DisconnectDevice()
    {
        if (ConnectedDevice != null)
        {
            if (_Adapter is null || _Adapter.State == State.Off)
            {
                _ConnectedDeviceGatt = null;
                ConnectedDevice = null;
            }
            else
            {
                _ConnectedDeviceGatt?.Disconnect();
            }
        }
    }

    #endregion

    #region Private Methods

    private BluetoothGattService BuildService(BLEService s)
    {
        var service = new BluetoothGattService(UUID.FromString(s.UUID), s.IsPrimary ? GattServiceType.Primary : GattServiceType.Secondary);

        foreach (var ch in s.Characteristics)
        {
            var characteristic = new BluetoothGattCharacteristic(UUID.FromString(ch.UUID), ConvertProperties(ch.Properties), ConvertPermission(ch.Permissions));
            service.AddCharacteristic(characteristic);
        }

        return service;
    }

    private GattProperty ConvertProperties(string properties) =>
        properties switch
        {
            "Read" => GattProperty.Read,
            "Read, Write" => GattProperty.Read | GattProperty.Write,
            "Write" => GattProperty.Write,
            "WriteWithoutResponse" => GattProperty.WriteNoResponse,
            "Indicate" => GattProperty.Indicate,
            "Notify" => GattProperty.Notify,
            _ => GattProperty.Read
        };

    private GattPermission ConvertPermission(BLEPermissions permissions) =>
        permissions switch
        {
            BLEPermissions.Readable => GattPermission.Read,
            BLEPermissions.Writeable => GattPermission.Write,
            BLEPermissions.ReadEncryptionRequired => GattPermission.ReadEncrypted,
            BLEPermissions.WriteEncryptionRequired => GattPermission.WriteEncrypted,
            _ => GattPermission.Read
        };

    private void AddPeripherals(string[] uuids)
    {
        if (uuids != null)
        {
            foreach (var uuid in uuids)
            {
                var cbuuid = UUID.FromString(uuid);
                if (cbuuid != null && !_PeripheralUUIDs.Contains(cbuuid))
                    _PeripheralUUIDs.Add(cbuuid);
            }
        }
    }

    #endregion

    #region Properties

    private BLEAdvertisingManager? _AdvertisingManager;
    public bool IsPoweredOn => _Adapter?.State == State.On;
    public bool CanAccess => _Adapter?.IsEnabled ?? false;
    public bool IsAdvertising => false;

    private int? _ManufacturerID;

    private BluetoothGatt? _ConnectedDeviceGatt;

    public event EventHandler<EventDataArgs<Packet>>? DeviceDiscovered = delegate { };
    public event EventHandler<EventDataArgs<BLEDeviceStatus>>? DeviceConnectionStatus = delegate { };
    public event EventHandler<EventArgs>? BluetoothStateChanged;

    public bool IsScanning { get; set; }

    public IConnectedDevice? ConnectedDevice { get; private set; }

    private readonly List<UUID> _PeripheralUUIDs = new();
    private readonly BluetoothManager _BluetoothManager;
    private readonly BluetoothAdapter? _Adapter;
    private GattServerCallback? _Manager;
    private BluetoothGattServer? _Server;
    private readonly Dictionary<string, BluetoothDevice>? _DevicesFound;

    #endregion
}