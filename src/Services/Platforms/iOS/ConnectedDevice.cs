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

        _Device.DiscoveredService += ServiceDiscovered;
        _Device.DiscoveredCharacteristics += CharacteristicsDiscovered;
        _Device.DiscoverServices();
    }

    public void Dispose()
    {
        Debug.WriteLine("ConnectedDevice: Disposing");
        if (_Device is null) return;
        _Device.DiscoveredService -= ServiceDiscovered;
        _Device.DiscoveredCharacteristics -= CharacteristicsDiscovered;
        _Device.UpdatedCharacterteristicValue -= CharacterteristicValueUpdated;
    }

    private void ServiceDiscovered(object? sender, NSErrorEventArgs e)
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

    private void CharacteristicsDiscovered(object? sender, CBServiceEventArgs e)
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

    public Task<IBLERequest> Write(IBLERequest request)
    {
        Debug.WriteLine($"ConnectedDevice: Write {(request.WithResponse ? "With Response" : "Without Response")}");

        var tcs = new TaskCompletionSource<IBLERequest>();
        var ct = new CancellationTokenSource(4000);

        if (request.WithResponse)
        {
            EventHandler<CBCharacteristicEventArgs>? withResponseCallback = null;
            withResponseCallback =
            (s, e) =>
            {
                Debug.WriteLine($"ConnectedDevice->Write - Response");
                if (request.ServiceID.Equals(e.Characteristic.Service.UUID.ToString(true), StringComparison.CurrentCultureIgnoreCase) && request.CharacteristicID.Equals(e.Characteristic.UUID.ToString(true), StringComparison.CurrentCultureIgnoreCase))
                {
                    ct.Dispose();
                    _Device.WroteCharacteristicValue -= withResponseCallback;
                    request.SetResponse(e.Characteristic.Value?.ToArray());
                    tcs.TrySetResult(request);
                }
            };

            ct.Token.Register(() =>
            {
                _Device.WroteCharacteristicValue -= withResponseCallback;
                tcs.TrySetCanceled();
            }, useSynchronizationContext: false);


            _Device.WroteCharacteristicValue += withResponseCallback;
        }
        else
        {
            EventHandler? withoutResponseCallback = null;
            withoutResponseCallback =
            (s, e) =>
            {
                Debug.WriteLine($"ConnectedDevice->Write - Response");
                ct.Dispose();
                _Device.IsReadyToSendWriteWithoutResponse -= withoutResponseCallback;
                request.SetResponse(null);
                tcs.TrySetResult(request);
            };

            ct.Token.Register(() =>
            {
                _Device.IsReadyToSendWriteWithoutResponse -= withoutResponseCallback;
                tcs.TrySetCanceled();
            }, useSynchronizationContext: false);

            _Device.IsReadyToSendWriteWithoutResponse += withoutResponseCallback;
        }

        var ch = GetCharacteristic(request.ServiceID, request.CharacteristicID);
        if (ch == null) return Task.FromException<IBLERequest>(new ArgumentException("Service or Characteristic not found"));
        _Device.WriteValue(NSData.FromArray(request.Data), ch, request.WithResponse ? CBCharacteristicWriteType.WithResponse : CBCharacteristicWriteType.WithoutResponse);
        return tcs.Task;
    }

    public Task<IBLERequest> Read(IBLERequest request)
    {

        Debug.WriteLine($"ConnectedDevice: Read");

        var tcs = new TaskCompletionSource<IBLERequest>();
        var ct = new CancellationTokenSource(4000);

        EventHandler<CBDescriptorEventArgs>? callback = null;
        callback =
        (s, e) =>
        {
            Debug.WriteLine($"ConnectedDevice->Read - Response");
            if (request.ServiceID.Equals(e.Descriptor.Characteristic.Service.UUID.ToString(), StringComparison.CurrentCultureIgnoreCase) && request.CharacteristicID.Equals(e.Descriptor.Characteristic.UUID.ToString(), StringComparison.CurrentCultureIgnoreCase))
            {
                ct.Dispose();
                _Device.UpdatedValue -= callback;
                request.SetResponse(e.Descriptor.Characteristic.Value?.ToArray());
                tcs.TrySetResult(request);
            }
        };

        ct.Token.Register(() =>
        {
            _Device.UpdatedValue -= callback;
            tcs.TrySetCanceled();
        }, useSynchronizationContext: false);

        var cbID = CBUUID.FromString(request.ServiceID);
        var cbIDString = cbID.Uuid;
        var ch = GetCharacteristic(cbIDString, request.CharacteristicID);
        if (ch == null)
            return Task.FromException<IBLERequest>(new ArgumentException($"Error - ConnectedDevice->Read: Service: {request.ServiceID} - CharId: {request.CharacteristicID} not found."));

        _Device.UpdatedValue += callback;
        _Device.ReadValue(ch);
        return tcs.Task;
    }

    public Task StartNotifying(string serviceID, string characteristicID)
    {
        Debug.WriteLine($"ConnectedDevice: StartNotifying");

        var cbID = CBUUID.FromString(serviceID);
        var cbIDString = cbID.Uuid;
        var ch = GetCharacteristic(cbIDString, characteristicID);
        if (ch == null)
            return Task.FromException(new ArgumentNullException($"ConnectedDevice->Read: Service: {serviceID} - CharId: {characteristicID} not found."));

        Debug.WriteLine("ConnectedDevice: Read - setting up read response");

        _Device.UpdatedCharacterteristicValue += CharacterteristicValueUpdated;
        _Device.SetNotifyValue(true, ch);
        return Task.CompletedTask; //this is due to android needing to pause after the notifications are set.
    }

    private void CharacterteristicValueUpdated(object? sender, CBCharacteristicEventArgs e)
    {
        Debug.WriteLine($"ConnectedDevice: Notification->CharacterteristicValueUpdated");
        CharacteristicChanged?.Invoke(this, new(new(e.Characteristic.Service.UUID.ToString(true), e.Characteristic.UUID.ToString(true), e.Characteristic.Value?.ToArray())));
    }

    public void StopNotifying(string serviceID, string characteristicID)
    {
        _Device.UpdatedCharacterteristicValue -= CharacterteristicValueUpdated;

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
    public event EventHandler<EventDataArgs<Tuple<string, string, byte[]>>>? CharacteristicChanged;

    public nuint MTU { get; private set; }
    public bool GattReady { get; private set; }
    public string Address { get; private set; }

    public event EventHandler? DeviceReady;
}