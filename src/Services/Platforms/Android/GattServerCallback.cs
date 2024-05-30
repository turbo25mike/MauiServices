using Android.Bluetooth;
using Android.Runtime;

namespace Turbo.Maui.Services.Platforms
{
	public class GattServerCallback: BluetoothGattServerCallback
	{
        public override void OnCharacteristicReadRequest(BluetoothDevice? device, int requestId, int offset, BluetoothGattCharacteristic? characteristic)
        {
            base.OnCharacteristicReadRequest(device, requestId, offset, characteristic);
            CharacteristicRead?.Invoke(this, new(device, requestId, offset, characteristic));
        }

        public override void OnCharacteristicWriteRequest(BluetoothDevice? device, int requestId, BluetoothGattCharacteristic? characteristic, bool preparedWrite, bool responseNeeded, int offset, byte[]? value)
        {
            base.OnCharacteristicWriteRequest(device, requestId, characteristic, preparedWrite, responseNeeded, offset, value);
            CharacteristicWrite?.Invoke(this, new(device, requestId, characteristic, preparedWrite, responseNeeded, offset, value));
        }

        public override void OnConnectionStateChange(BluetoothDevice? device, [GeneratedEnum] ProfileState status, [GeneratedEnum] ProfileState newState)
        {
            base.OnConnectionStateChange(device, status, newState);
            ConnectionStateChange?.Invoke(this, new(device, status, newState));
        }

        public override void OnDescriptorReadRequest(BluetoothDevice? device, int requestId, int offset, BluetoothGattDescriptor? descriptor)
        {
            base.OnDescriptorReadRequest(device, requestId, offset, descriptor);
            DescriptorRead?.Invoke(this, new(device, requestId, offset, descriptor));
        }

        public override void OnDescriptorWriteRequest(BluetoothDevice? device, int requestId, BluetoothGattDescriptor? descriptor, bool preparedWrite, bool responseNeeded, int offset, byte[]? value)
        {
            base.OnDescriptorWriteRequest(device, requestId, descriptor, preparedWrite, responseNeeded, offset, value);
            DescriptorWrite?.Invoke(this, new(device, requestId, descriptor, preparedWrite, responseNeeded, offset, value));
        }

        public override void OnExecuteWrite(BluetoothDevice? device, int requestId, bool execute)
        {
            base.OnExecuteWrite(device, requestId, execute);
            ExecuteWrite?.Invoke(this, new(device, requestId, execute));
        }

        public override void OnMtuChanged(BluetoothDevice? device, int mtu)
        {
            base.OnMtuChanged(device, mtu);
            MtuChanged?.Invoke(this, new(device, mtu));
        }

        public override void OnNotificationSent(BluetoothDevice? device, [GeneratedEnum] GattStatus status)
        {
            base.OnNotificationSent(device, status);
            NotificationSent?.Invoke(this, new(device, status));
        }

        public override void OnServiceAdded([GeneratedEnum] GattStatus status, BluetoothGattService? service)
        {
            base.OnServiceAdded(status, service);
            ServiceAdded?.Invoke(this, new(status, service));
        }

        public event EventHandler<ExecuteWriteServerEventArgs>? ExecuteWrite;
        public event EventHandler<DescriptorReadServerEventArgs>? DescriptorRead;
        public event EventHandler<DescriptorWriteServerEventArgs>? DescriptorWrite;
        public event EventHandler<ConnectionStateChangeServerEventArgs>? ConnectionStateChange;
        public event EventHandler<CharacteristicReadServerEventArgs>? CharacteristicRead;
        public event EventHandler<CharacteristicWriteServerEventArgs>? CharacteristicWrite;
        public event EventHandler<MtuChangedServerEventArgs>? MtuChanged;
        public event EventHandler<NotificationSentServerEventArgs>? NotificationSent;
        public event EventHandler<ServiceAddedServerEventArgs>? ServiceAdded;
    }

    public class ServiceAddedServerEventArgs
    {
        public ServiceAddedServerEventArgs(GattStatus status, BluetoothGattService? service)
        {
            Service = service;
            Status = status;
        }

        public BluetoothGattService? Service { get; private set; }
        public GattStatus Status { get; private set; }
    }

    public class NotificationSentServerEventArgs
    {
        public NotificationSentServerEventArgs(BluetoothDevice? device, GattStatus status)
        {
            Device = device;
            Status = status;
        }

        public BluetoothDevice? Device { get; private set; }
        public GattStatus Status { get; private set; }
    }

    public class MtuChangedServerEventArgs
    { 
        public MtuChangedServerEventArgs(BluetoothDevice? device, int mtu)
        {
            Device = device;
            Mtu = mtu;
        }

        public BluetoothDevice? Device { get; private set; }
        public int Mtu { get; private set; }
    }

    public class ExecuteWriteServerEventArgs
    {
        public ExecuteWriteServerEventArgs(BluetoothDevice? device, int requestId, bool execute)
        {
            Device = device;
            RequestID = requestId;
            Execute = execute;
        }

        public BluetoothDevice? Device { get; private set; }
        public int RequestID { get; private set; }
        public bool Execute { get; private set; }
    }

    public class DescriptorWriteServerEventArgs
    {
        public DescriptorWriteServerEventArgs(BluetoothDevice? device, int requestId, BluetoothGattDescriptor? descriptor, bool preparedWrite, bool responseNeeded, int offset, byte[]? value)
        {
            Device = device;
            Descriptor = descriptor;
            RequestID = requestId;
            Offset = offset;
            PreparedWrite = preparedWrite;
            ResponseNeeded = responseNeeded;
            Value = value;
        }

        public BluetoothDevice? Device { get; private set; }
        public int RequestID { get; private set; }
        public int Offset { get; private set; }
        public BluetoothGattDescriptor? Descriptor { get; private set; }
        public bool PreparedWrite { get; private set; }
        public bool ResponseNeeded { get; private set; }
        public byte[]? Value { get; private set; }
    }

    public class DescriptorReadServerEventArgs
    {
        public DescriptorReadServerEventArgs(BluetoothDevice? device, int requestId, int offset, BluetoothGattDescriptor? descriptor)
        {
            Device = device;
            RequestID = requestId;
            Offset = offset;
            Descriptor = descriptor;
        }

        public BluetoothDevice? Device { get; private set; }
        public BluetoothGattCharacteristic? Characteristic { get; private set; }
        public int RequestID { get; private set; }
        public int Offset { get; private set; }
        public BluetoothGattDescriptor? Descriptor { get; private set; }
    }

    public class ConnectionStateChangeServerEventArgs
    {
        public ConnectionStateChangeServerEventArgs(BluetoothDevice? device, [GeneratedEnum] ProfileState status, [GeneratedEnum] ProfileState newState)
        {
            Device = device;
            Status = status;
            NewState = newState;
        }

        public BluetoothDevice? Device { get; private set; }
        public ProfileState Status { get; private set; }
        public ProfileState NewState { get; private set; }
    }

    public class CharacteristicWriteServerEventArgs
    {
        public CharacteristicWriteServerEventArgs(BluetoothDevice? device, int requestId, BluetoothGattCharacteristic? characteristic, bool preparedWrite, bool responseNeeded, int offset, byte[]? value)
        {
            Device = device;
            Characteristic = characteristic;
            RequestID = requestId;
            Offset = offset;
            PreparedWrite = preparedWrite;
            ResponseNeeded = responseNeeded;
            Value = value;
        }


        public BluetoothDevice? Device { get; private set; }
        public BluetoothGattCharacteristic? Characteristic { get; private set; }
        public int RequestID { get; private set; }
        public int Offset { get; private set; }
        public bool PreparedWrite { get; private set; }
        public bool ResponseNeeded { get; private set; }
        public byte[]? Value { get; private set; }
    }

    public class CharacteristicReadServerEventArgs
    {
        public CharacteristicReadServerEventArgs(BluetoothDevice? device, int requestId, int offset, BluetoothGattCharacteristic? characteristic)
        {
            Device = device;
            Characteristic = characteristic;
            RequestID = requestId;
            Offset = offset;
        }

        public BluetoothDevice? Device { get; private set; }
        public BluetoothGattCharacteristic? Characteristic { get; private set; }
        public int RequestID { get; private set; }
        public int Offset { get; private set; }
    }
}

