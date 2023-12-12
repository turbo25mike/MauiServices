using Turbo.Maui.Services.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Turbo.Maui.Services;
using System.Collections.ObjectModel;
using System.Text;
using System.Diagnostics;
using BluetoothService.Example.Utilities;

namespace BluetoothService.Example;

public partial class DeviceViewModel : ObservableObject, IQueryAttributable
{
    public DeviceViewModel(IBluetoothService bluetoothService)
    {
        _BluetoothService = bluetoothService;
        _BluetoothService.DeviceConnectionStatus += BluetoothService_DeviceConnectionStatus;
        _BluetoothService.DeviceMessageReceived += BluetoothService_DeviceMessageReceived;
    }

    private void BluetoothService_DeviceMessageReceived(object? sender, DeviceMessageBytesEventArgs e)
    {
        if (e.Response == null) return;
        var rawData = Encoding.ASCII.GetString(e.Response);
        Debug.WriteLine($"Data Received: {rawData}", false);
    }

    private async void BluetoothService_DeviceConnectionStatus(object? sender, EventDataArgs<BLEDeviceStatus> e)
    {
        ConnectionStatus = e.Data.ToString();

        switch (e.Data)
        {
            case BLEDeviceStatus.Connected:

                await _BluetoothService.SetNotifications(DATA_SERVICE, NOTIFY_CHARACTERISTIC);
                SendToDevice(1000, null);
                //Services.Clear();
                //var svcs = _BluetoothService.ConnectedDevice.GetServices();
                //foreach (var svc in svcs)
                //    Services.Add(svc);
                break;
            case BLEDeviceStatus.Disconnected:
                //await Shell.Current.GoToAsync("..", true);
                break;
        }
    }

    private void SendToDevice(uint register, uint? value = null)
    {
        var data = Encoding.UTF8.GetBytes(Radix64Util.Generate(register, 1, value));
        var request = new BLERequest(DATA_SERVICE, WRITE_CHARACTERISTIC, data);
        _BluetoothService.Write(request);
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query["device"] is PacketExt device)
            SelectedDevice = device;

        if (SelectedDevice == null) return;

        _BluetoothService.ConnectTo(SelectedDevice.ID);
    }

    [ObservableProperty]
    private PacketExt? _SelectedDevice;

    [ObservableProperty]
    private string _ConnectionStatus = "Unknown";

    [ObservableProperty]
    private ObservableCollection<BLEService> _Services = new();

    private readonly IBluetoothService _BluetoothService;

    public const string DATA_SERVICE = "38400001-7537-4A20-B134-6762E12627BF";
    public const string NOTIFY_CHARACTERISTIC = "38400003-7537-4A20-B134-6762E12627BF";
    public const string WRITE_CHARACTERISTIC = "38400002-7537-4A20-B134-6762E12627BF";
}