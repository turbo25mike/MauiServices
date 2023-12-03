using Turbo.Maui.Services.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Turbo.Maui.Services;
using System.Collections.ObjectModel;

namespace BluetoothService.Example;

public partial class DeviceViewModel : ObservableObject, IQueryAttributable
{
    public DeviceViewModel(IBluetoothService bluetoothService)
    {
        _BluetoothService = bluetoothService;
        _BluetoothService.DeviceConnectionStatus += BluetoothService_DeviceConnectionStatus;
    }

    private async void BluetoothService_DeviceConnectionStatus(object? sender, EventDataArgs<BLEDeviceStatus> e)
    {
        ConnectionStatus = e.Data.ToString();

        switch (e.Data)
        {
            case BLEDeviceStatus.Connected:

                Services.Clear();
                var svcs = _BluetoothService.ConnectedDevice.GetServices();
                foreach (var svc in svcs)
                    Services.Add(svc);
                break;
            case BLEDeviceStatus.Disconnected:
                await Shell.Current.GoToAsync("..", true);
                break;
        }
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
}