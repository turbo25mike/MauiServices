using System.Diagnostics;
using CoreBluetooth;
using Foundation;

namespace Turbo.Maui.Services.Platforms;

public class ConnectedDevice : IConnectedDevice
{
    public ConnectedDevice(CBPeripheral device)
    {
        Console.WriteLine($"ConnectedDevice: Init({device.Name})");

        _Device = device;
        Address = _Device.Identifier.ToString();
        MTU = 20; //Default value till we are connected;

        _Device.IsReadyToSendWriteWithoutResponse += (s, e) => IsReadyToSendWriteWithoutResponse?.Invoke(this, new EventArgs());

        _Device.DiscoveredService += (c, v) =>
        {
            if (_Device.Services == null) return;
            Debug.WriteLine($"ConnectedDevice: Service(s) {_Device.Services.Length} Discovered");
            foreach (var svc in _Device.Services)
            {
                Debug.WriteLine($"Service: {svc.UUID}");
                if (svc.Characteristics == null)
                    _Device.DiscoverCharacteristics(svc);
            }
        };

        _Device.DiscoveredCharacteristics += (j, i) =>
        {
            try
            {
                Console.WriteLine($"ConnectedDevice: Characteristic Discovered");
                var serviceCharFound = 0;
                if (_Device.Services == null)
                {
                    Debug.WriteLine($"ConnectedDevice: 0 Characteristics Found");
                    return;
                }
                foreach (var svc in _Device.Services)
                    if (svc.Characteristics != null)
                        serviceCharFound++;
                if (serviceCharFound == _Device.Services.Length)
                {
                    MTU = _Device.GetMaximumWriteValueLength(CBCharacteristicWriteType.WithoutResponse) - 3;
                    GattReady = true;
                    DeviceReady?.Invoke(this, new EventArgs());
                }
                Console.WriteLine($"ConnectedDevice: Characteristic(s) {serviceCharFound} Found");
            }
            catch (Exception)
            {
            }
        };

        _Device.DiscoverServices();
    }

    public void Write(string serviceID, string characteristicId, byte[] val, bool withResponse = true)
    {
        var ch = GetCharacteristic(serviceID, characteristicId);
        if (ch == null) return;
        _Device.WriteValue(NSData.FromArray(val), ch, (withResponse) ? CBCharacteristicWriteType.WithResponse : CBCharacteristicWriteType.WithoutResponse);
        CharacteristicWrite?.Invoke(this, new EventArgs());
    }

    public void Read(string serviceID, string characteristicID, Action<KeyValuePair<string, byte[]?>> action, bool notify)
    {
        var cbID = CBUUID.FromString(serviceID);
        var cbIDString = cbID.Uuid;
        var ch = GetCharacteristic(cbIDString, characteristicID);
        if (ch == null)
        {
            Debug.WriteLine($"Error - ConnectedDevice->Read: Service: {serviceID} - CharId: {characteristicID} not found.");
            return;
        }

        _Device.UpdatedCharacterteristicValue += (sender, e) => action.Invoke(new KeyValuePair<string, byte[]?>(Address, e.Characteristic.Value?.ToArray()));
        _Device.UpdatedValue += (sender, e) => action.Invoke(new KeyValuePair<string, byte[]?>(Address, (e.Descriptor.Value as NSData)?.ToArray()));
        if (notify)
        {
            _Device.SetNotifyValue(true, ch);
        }
        else
        {
            _Device.ReadValue(ch);
        }
    }

    public void StopNotifying(string serviceID, string characteristicID)
    {
        var ch = GetCharacteristic(serviceID, characteristicID);
        if (ch == null) return;
        _Device.SetNotifyValue(false, ch);
    }

    public IEnumerable<BLEService> GetServices()
    {
        var results = new List<BLEService>();

        if (_Device.Services == null) return results;

        foreach (var svc in _Device.Services)
        {
            var chList = new List<BLECharacteristic>();
            if (svc.Characteristics != null)
                foreach (var ch in svc.Characteristics)
                    chList.Add(new(ch.UUID.ToString().ToUpper(), ch.Properties.ToString()));

            results.Add(new(svc.UUID.ToString().ToUpper(), chList));
        }
        return results;
    }

    public bool HasService(string serviceID) => _Device.Services?.FirstOrDefault(x => x.UUID.ToString().ToUpper() == serviceID.ToUpper()) != null;

    public bool HasCharacteristic(string serviceID, string characteristicID) => GetCharacteristic(serviceID, characteristicID) != null;

    public bool CanSendWriteWithoutResponse() => _Device.CanSendWriteWithoutResponse;

    public bool CanSendWrite() => true;

    public Task<nuint> RequestMTU(int size) => Task.FromResult(_Device.GetMaximumWriteValueLength(CBCharacteristicWriteType.WithoutResponse));

    CBCharacteristic? GetCharacteristic(string serviceID, string characteristicID)
    {
        var svc = _Device.Services?.FirstOrDefault(x => Equals(x.UUID, CBUUID.FromString(serviceID)));
        if (svc == null || svc.Characteristics == null) return null;
        return svc.Characteristics?.FirstOrDefault(x => Equals(x.UUID, CBUUID.FromString(characteristicID))) ?? null;
    }

    private readonly CBPeripheral _Device;

    public nuint MTU { get; private set; }
    public bool GattReady { get; private set; }
    public string Address { get; private set; }

    public event EventHandler? CharacteristicWrite;
    public event EventHandler? DeviceReady;
    public event EventHandler? IsReadyToSendWriteWithoutResponse;
}