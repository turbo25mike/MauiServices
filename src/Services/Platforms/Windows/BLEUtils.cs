namespace Turbo.Maui.Services.Platforms;

public static class BLEUtils
{
    /// <summary>
    /// Method to parse the bluetooth address as a hex string to a UUID
    /// </summary>
    /// <param name="bluetoothAddress">BluetoothLEDevice native device address</param>
    /// <returns>a GUID that is padded left with 0 and the last 6 bytes are the bluetooth address</returns>
    public static Guid ParseDeviceId(ulong bluetoothAddress)
    {
        var macWithoutColons = bluetoothAddress.ToString("x");
        macWithoutColons = macWithoutColons.PadLeft(12, '0'); //ensure valid length
        var deviceGuid = new byte[16];
        Array.Clear(deviceGuid, 0, 16);
        var macBytes = Enumerable.Range(0, macWithoutColons.Length)
            .Where(x => x % 2 == 0)
            .Select(x => Convert.ToByte(macWithoutColons.Substring(x, 2), 16))
            .ToArray();
        macBytes.CopyTo(deviceGuid, 10);
        return new Guid(deviceGuid);
    }
}
