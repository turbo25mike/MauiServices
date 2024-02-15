using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Turbo.Maui.Services;
using Turbo.Maui.Services.Models;

namespace BluetoothService.Example;

public partial class MainViewModel : ObservableObject
{
    public MainViewModel(IBluetoothService bluetoothService)
    {
        _BluetoothService = bluetoothService;
        _BluetoothService.PacketDiscovered += PacketDiscovered;
        _BluetoothService.BluetoothStateChanged += (s, e) => CheckStatus();

        CheckStatus();
    }

    [RelayCommand]
    private async Task GetData()
    {
        if (await CheckPermissions() && CheckStatus())
        {
            if (!_BluetoothService.IsScanning)
            {
                Status = "Scanning...";
                //_BluetoothService.Scan();  //open for all BLE devices
                _BluetoothService.Scan(new[] { SERVICEA, SERVICEB }, 2800); //locked down to specific BLE devices
                ButtonText = "Stop";
            }
            else
            {
                Status = "Stopped";
                ButtonText = "Scan";
                _BluetoothService.Stop();
            }
        }
    }


    [RelayCommand]
    private async Task Connect(PacketExt selectedDevice)
    {
        await Shell.Current.GoToAsync(nameof(DevicePage), new Dictionary<string, object>() { { "device", selectedDevice } });
    }

    private bool CheckStatus()
    {
        if (_BluetoothService.IsPoweredOn)
        {
            Status = "Powered On";
            BluetoothEnabled = true;
            return true;
        }
        else
        {
            Status = "Powered Off";
            BluetoothEnabled = false;
            return false;
        }
    }

    private async Task<bool> CheckPermissions()
    {
        var permissions = new BluetoothPermissions();
        var permissionStatus = await permissions.CheckStatusAsync();
        if (permissionStatus != PermissionStatus.Granted)
        {
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.DisplayAlert("Permissions", "Our app needs your permission to ... Please allow access when prompted.", "Continue");
            permissionStatus = await permissions.RequestAsync();
        }

        Status = $"Access {PermissionStatus.Granted}";
        if (permissionStatus == PermissionStatus.Granted)
            return true;

        ButtonText = "Request Permission";
        return false;
    }

    private void PacketDiscovered(object? sender, EventDataArgs<Packet> e)
    {
        Debug.WriteLine($"Packet Discovered Name: {e.Data.Name} {DateTime.Now.ToLongTimeString()}");
        
        var found = FoundDevices.FirstOrDefault(x => x.ID == e.Data.ID);
        if (found == null)
        {
            var pE = new PacketExt(e.Data);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                FoundDevices.Add(pE);
            });
        }
        else
            MainThread.BeginInvokeOnMainThread(() =>
            {
                found.Update(e.Data);
            });
    }


    [ObservableProperty]
    private string _Status = "Awaiting User";

    [ObservableProperty]
    private ObservableCollection<PacketExt> _FoundDevices = new();

    [ObservableProperty]
    private string _ButtonText = "Scan";

    [ObservableProperty]
    private bool _BluetoothEnabled = false;

    private readonly IBluetoothService _BluetoothService;

    public const string SERVICEA = "0000FCF9-0000-1000-8000-00805F9B34FB";
    public const string SERVICEB = "0000FE59-0000-1000-8000-00805F9B34FB";
}

public partial class PacketExt : Packet
{
    public PacketExt(Packet p)
    {
        ID = p.ID;
        Update(p);
    }

    public void Update(Packet p)
    {
        LastSeen = DateTime.Now;
        RSSI = p.RSSI;
        if (!string.IsNullOrWhiteSpace(p.Name) && (Name == "- No Name -" || string.IsNullOrWhiteSpace(Name)))
            Name = p.Name;
        else if(string.IsNullOrWhiteSpace(Name))
            Name = "- No Name -";
        TxPower = p.TxPower;
        ManufacturerData = p.ManufacturerData;
        ServiceData = p.ServiceData;
        ManufacturerDataString = GetString(p.ManufacturerData);
        ServiceDataString = GetString(p.ServiceData);
    }

    private string GetString(byte[] bytes) => bytes == null ? "" : $"{string.Join(", ", bytes)}";

    [ObservableProperty]
    private string _ManufacturerDataString = "";

    [ObservableProperty]
    private string _ServiceDataString = "";


    [ObservableProperty]
    private DateTime _LastSeen;
}