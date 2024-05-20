namespace Turbo.Maui.Services.Models;

public class BLEAdvertisingManager
{
    public BLEAdvertisingManager(string localName, BLEService[] services)
    {
        LocalName = localName;
        Services = services;
    }
    
    public string? ReadRequested(string deviceID, string serviceID, string characteristicID) => GetCharacteristic(serviceID, characteristicID)?.ReadRequest?.Invoke(deviceID);

    public void WriteRequested(string deviceID, string serviceID, string characteristicID, string value) => GetCharacteristic(serviceID, characteristicID)?.WriteRequest?.Invoke(deviceID, value);

    public void Unsubscribed(string deviceID, string serviceID, string characteristicID) => GetCharacteristic(serviceID, characteristicID)?.Unsubscribed?.Invoke(deviceID);

    public void Subscribed(string deviceID, string serviceID, string characteristicID) => GetCharacteristic(serviceID, characteristicID)?.Subscribed?.Invoke(deviceID);

    private BLECharacteristic? GetCharacteristic(string serviceID, string characteristicID) => Services.FirstOrDefault(s => s.UUID.Equals(serviceID, StringComparison.CurrentCultureIgnoreCase))?.GetCharacteristic(characteristicID);
    
    public BLEService[] Services { get; private set; }
    public string LocalName { get; private set; }
}