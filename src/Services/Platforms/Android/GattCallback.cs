using Android.Bluetooth;
using Android.Runtime;

namespace Turbo.Maui.Services.Platforms;

public class GattCallback : BluetoothGattCallback
{
    public override void OnConnectionStateChange(BluetoothGatt? gatt, GattStatus status, ProfileState newState)
    {
        base.OnConnectionStateChange(gatt, status, newState);
        DeviceStatus?.Invoke(this, new(DeviceStateUtil.Convert(newState)));
    }

    public override void OnServicesDiscovered(BluetoothGatt? gatt, GattStatus status)
    {
        base.OnServicesDiscovered(gatt, status);
        Console.WriteLine("OnServicesDiscovered: " + status.ToString());
        ServicesDiscovered?.Invoke(this, new ServicesDiscoveredEventArgs());
    }

    /// <summary>
    /// ANDROID greater than or equal to 33
    /// </summary>
    /// <param name="gatt"></param>
    /// <param name="characteristic"></param>
    /// <param name="value"></param>
    /// <param name="status"></param>
    public override void OnCharacteristicRead(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, byte[] value, [GeneratedEnum] GattStatus status)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            base.OnCharacteristicRead(gatt, characteristic, value, status);
            CharacteristicRead?.Invoke(this, new(value));
        }
    }

    /// <summary>
    /// ANDROID less than 33
    /// </summary>
    /// <param name="gatt"></param>
    /// <param name="characteristic"></param>
    /// <param name="status"></param>
    public override void OnCharacteristicRead(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic, GattStatus status)
    {
        if (characteristic is null) return;
        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            base.OnCharacteristicRead(gatt, characteristic, status);
            var result = characteristic.GetValue();
            if (result != null)
                CharacteristicRead?.Invoke(this, new(result));
        }
    }

    /// <summary>
    /// ANDROID less than 33
    /// </summary>
    /// <param name="gatt"></param>
    /// <param name="characteristic"></param>
    /// <param name="status"></param>
    public override void OnCharacteristicWrite(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic, GattStatus status)
    {
        base.OnCharacteristicWrite(gatt, characteristic, status);
        CharacteristicWrite?.Invoke(this, new());
        //Console.WriteLine($"GattCallback->OnCharacteristicWrite");
    }

    /// <summary>
    /// ANDROID greater than or equal to 33
    /// </summary>
    /// <param name="gatt"></param>
    /// <param name="characteristic"></param>
    /// <param name="value"></param>
    public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, byte[] value)
    {

        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            base.OnCharacteristicChanged(gatt, characteristic, value);
            CharacteristicChanged?.Invoke(this, new(value));
        }
    }

    public override void OnCharacteristicChanged(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic)
    {
        if (characteristic is null) return;
        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            base.OnCharacteristicChanged(gatt, characteristic);
            var bytes = characteristic.GetValue();
            if (bytes != null)
                CharacteristicChanged?.Invoke(this, new(bytes));
        }
    }

    public override void OnMtuChanged(BluetoothGatt? gatt, int mtu, [GeneratedEnum] GattStatus status)
    {
        base.OnMtuChanged(gatt, mtu, status);
        MtuChanged?.Invoke(this, new((nuint)mtu));
    }

    public event EventHandler<ServicesDiscoveredEventArgs>? ServicesDiscovered;
    public event EventHandler<EventDataArgs<BLEDeviceStatus>>? DeviceStatus;
    public event EventHandler<EventDataArgs<byte[]>>? CharacteristicChanged;
    public event EventHandler<EventDataArgs<byte[]>>? CharacteristicRead;
    public event EventHandler? CharacteristicWrite;
    public event EventHandler<EventDataArgs<nuint>>? MtuChanged;
}

public class CharacteristicReadEventArgs
{
}

public class ServicesDiscoveredEventArgs
{
}

public static class DeviceStateUtil
{
    public static BLEDeviceStatus Convert(ProfileState state)
    {
        switch (state)
        {
            case ProfileState.Disconnected: return BLEDeviceStatus.Disconnected;
            case ProfileState.Connecting: return BLEDeviceStatus.Connecting;
            case ProfileState.Connected: return BLEDeviceStatus.Connected;
            case ProfileState.Disconnecting: return BLEDeviceStatus.Disconnecting;
            default: return BLEDeviceStatus.Disconnected;
        }
    }
}