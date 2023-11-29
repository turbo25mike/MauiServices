using CommunityToolkit.Mvvm.ComponentModel;

namespace Turbo.Maui.Services.Models;

public partial class DeviceState : ObservableObject
{
    public SecurityState Security = new();

    public string Address { get; set; }
    public int SignalStrength { get; set; }
    public int CurrentSequenceNumber { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsConnected))]
    private BLEDeviceStatus _Status;

    [ObservableProperty]
    private DeviceMode _Mode = DeviceMode.Default;

    public bool IsConnected => Status == BLEDeviceStatus.Connected;

    public DeviceState() { }

    public DeviceState(string address)
    {
        Address = address;
    }

    public void TouchSequence()
    {
        CurrentSequenceNumber++;
        if (CurrentSequenceNumber == 64)
            CurrentSequenceNumber = 0;
    }
}

public enum DeviceMode { Default, DFU, Pairing }

public class SecurityState
{
    public string DeviceCode;
    public uint AccessCode;

    private uint _ChallengeCode;
    public uint ChallengeCode
    {
        get => _ChallengeCode;
        set
        {
            _ChallengeCode = value;
            DeviceCode = SecurityUtil.GetAlphaCode(_ChallengeCode);
        }
    }

    public uint Attempts = 0;
}