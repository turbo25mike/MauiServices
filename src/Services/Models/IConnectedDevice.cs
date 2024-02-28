namespace Turbo.Maui.Services.Models;

public interface IConnectedDevice
{
    bool GattReady { get; }
    event EventHandler DeviceReady;
    event EventHandler IsReadyToSendWriteWithoutResponse;
    event EventHandler CharacteristicWrite;

    nuint MTU { get; }
    Task<nuint> RequestMTU(int size);
    bool CanSendWriteWithoutResponse();
    bool CanSendWrite();
    string Address { get; }
    void Write(string serviceID, string characteristicID, byte[] val, bool withResponse = true);
    void Read(string serviceID, string characteristicID, Action<KeyValuePair<string, byte[]>> action, bool notify);
    void StopNotifying(string serviceID, string characteristicID);
    bool HasService(string serviceID);
    void Dispose();
    IEnumerable<BLEService> GetServices();
    bool HasCharacteristic(string serviceID, string characteristicID);
}