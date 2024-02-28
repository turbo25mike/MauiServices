using System.Diagnostics;
using CoreBluetooth;
using Foundation;

namespace Turbo.Maui.Services.Platforms;

public class ConnectedDevice : IConnectedDevice
{
    public ConnectedDevice(CBPeripheral device)
    {
        Debug.WriteLine($"ConnectedDevice: Init({device.Name})");

        _Device = device;
        Address = _Device.Identifier.ToString();
        MTU = 20; //Default value till we are connected;

        _Device.IsReadyToSendWriteWithoutResponse += DeviceIsReadyToSendWriteWithoutResponse;
        _Device.DiscoveredService += ServiceDiscovered;
        _Device.DiscoveredCharacteristics += CharacteristicsDiscovered;
        _Device.DiscoverServices();
    }

    public void Dispose()
    {
        Debug.WriteLine("ConnectedDevice: Disposing");
        _Device.IsReadyToSendWriteWithoutResponse -= DeviceIsReadyToSendWriteWithoutResponse;
        _Device.DiscoveredService -= ServiceDiscovered;
        _Device.DiscoveredCharacteristics -= CharacteristicsDiscovered;
        _Device.UpdatedCharacterteristicValue -= CharacterteristicValueUpdated;
        _Device.UpdatedValue -= PeripheralUpdatedValue;
    }

    private void DeviceIsReadyToSendWriteWithoutResponse(object sender, EventArgs e) => IsReadyToSendWriteWithoutResponse?.Invoke(this, new EventArgs());

    private void ServiceDiscovered(object sender, NSErrorEventArgs e)
    {
        if (_Device.Services == null) return;
        Debug.WriteLine($"ConnectedDevice: Service(s) {_Device.Services.Length} Discovered");
        foreach (var svc in _Device.Services)
        {
            Debug.WriteLine($"Service: {svc.UUID}");
            if (svc.Characteristics == null)
                _Device.DiscoverCharacteristics(svc);
        }
    }

    private void CharacteristicsDiscovered(object sender, CBServiceEventArgs e)
    {
        try
        {
            Debug.WriteLine($"ConnectedDevice: Characteristic Discovered");
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
            Debug.WriteLine($"ConnectedDevice: Characteristic(s) {serviceCharFound} Found");
        }
        catch (Exception)
        {
        }
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
        _NotificationOccured = action;
        var cbID = CBUUID.FromString(serviceID);
        var cbIDString = cbID.Uuid;
        var ch = GetCharacteristic(cbIDString, characteristicID);
        if (ch == null)
        {
            Debug.WriteLine($"Error - ConnectedDevice->Read: Service: {serviceID} - CharId: {characteristicID} not found.");
            return;
        }

        Debug.WriteLine("ConnectedDevice: Read - setting up read response");

        _Device.UpdatedCharacterteristicValue += CharacterteristicValueUpdated;
        _Device.UpdatedValue += PeripheralUpdatedValue;
        if (notify)
        {
            _Device.SetNotifyValue(true, ch);
        }
        else
        {
            _Device.ReadValue(ch);
        }
    }

    private void PeripheralUpdatedValue(object sender, CBDescriptorEventArgs e)
    {

        Debug.WriteLine($"ConnectedDevice: PeripheralUpdatedValue");
        _NotificationOccured?.Invoke(new KeyValuePair<string, byte[]>(Address, (e.Descriptor.Value as NSData)?.ToArray()));
    }

    private void CharacterteristicValueUpdated(object sender, CBCharacteristicEventArgs e)
    {
        Debug.WriteLine($"ConnectedDevice: CharacterteristicValueUpdated");
        _NotificationOccured?.Invoke(new KeyValuePair<string, byte[]>(Address, e.Characteristic.Value?.ToArray()));
    }

    public void StopNotifying(string serviceID, string characteristicID)
    {
        Debug.WriteLine($"ConnectedDevice: StopNotifying");
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
    private Action<KeyValuePair<string, byte[]>> _NotificationOccured;

    public nuint MTU { get; private set; }
    public bool GattReady { get; private set; }
    public string Address { get; private set; }

    public event EventHandler? CharacteristicWrite;
    public event EventHandler? DeviceReady;
    public event EventHandler? IsReadyToSendWriteWithoutResponse;
}