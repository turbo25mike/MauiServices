namespace Turbo.Maui.Services.Models;

public interface IConnectedDevice
{
    bool GattReady { get; }
    event EventHandler DeviceReady;
    event EventHandler<EventDataArgs<Tuple<string, string, byte[]>>>? CharacteristicChanged;

    nuint MTU { get; }
    Task<nuint> RequestMTU(int size);
    string Address { get; }
    Task<IBLERequest> Write(IBLERequest request);
    Task<IBLERequest> Read(IBLERequest request);
    Task StartNotifying(string serviceID, string characteristicID);
    void StopNotifying(string serviceID, string characteristicID);
    bool HasService(string serviceID);
    void Dispose();
    IEnumerable<BLEService> GetServices();
    bool HasCharacteristic(string serviceID, string characteristicID);
}