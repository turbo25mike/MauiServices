using Turbo.Maui.Services.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Turbo.Maui.Services;
using System.Collections.ObjectModel;
using System.Text;
using BluetoothService.Example.Utilities;
using CommunityToolkit.Mvvm.Input;

namespace BluetoothService.Example;

public partial class DeviceViewModel : ObservableObject, IQueryAttributable
{
    public DeviceViewModel(IBluetoothService bluetoothService)
    {
        _BluetoothService = bluetoothService;
        _BluetoothService.DeviceConnectionStatus += BluetoothService_DeviceConnectionStatus;
        _BluetoothService.NotificationReceived += BluetoothService_NotificationReceived;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Messages = "";
        if (query["device"] is PacketExt device)
            SelectedDevice = device;

        if (SelectedDevice == null) throw new ArgumentNullException(nameof(query));
        _BluetoothService.ConnectTo(SelectedDevice.ID);
    }

    [RelayCommand]
    private void Disconnect() => _BluetoothService.DisconnectDevice();

    private void BluetoothService_NotificationReceived(object? sender, EventDataArgs<Tuple<string, string, byte[]>> e) =>
        Messages += $"Notification: {Encoding.ASCII.GetString(e.Data.Item3)}\n";

    private void BluetoothService_DeviceConnectionStatus(object? sender, EventDataArgs<BLEDeviceStatus> e)
    {
        Messages += $"Device Status: {e.Data}\n";
        switch (e.Data)
        {
            case BLEDeviceStatus.Connected:
                _BluetoothService.StartNotifying(DATA_SERVICE, NOTIFY_CHARACTERISTIC);
                SendToDevice(1000, null);
                //Services.Clear();
                //var svcs = _BluetoothService.ConnectedDevice.GetServices();
                //foreach (var svc in svcs)
                //    Services.Add(svc);
                break;
            case BLEDeviceStatus.Disconnected:
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Shell.Current.GoToAsync("..", true);
                });
                break;
        }
    }

    private async void SendToDevice(uint register, uint? value = null)
    {
        var data = Encoding.UTF8.GetBytes(Radix64Util.Generate(register, 1, value));
        var request = new BLERequest(DATA_SERVICE, WRITE_CHARACTERISTIC, data);
        await _BluetoothService.Write(request);
        Messages += $"Write Response: {request.Response}\n";
    }

    [ObservableProperty]
    private PacketExt? _SelectedDevice;

    [ObservableProperty]
    private string _Messages = "";

    [ObservableProperty]
    private ObservableCollection<BLEService> _Services = new();

    private readonly IBluetoothService _BluetoothService;

    public const string DATA_SERVICE = "38400001-7537-4A20-B134-6762E12627BF";
    public const string NOTIFY_CHARACTERISTIC = "38400003-7537-4A20-B134-6762E12627BF";
    public const string WRITE_CHARACTERISTIC = "38400002-7537-4A20-B134-6762E12627BF";
}