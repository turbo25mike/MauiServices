using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Turbo.Maui.Services.Models;

public partial class BLEService : ObservableObject
{
    public BLEService(string id, IEnumerable<BLECharacteristic> characteristics, bool isPrimary = false)
    {
        UUID = id;
        IsPrimary = isPrimary;
        Characteristics = new();
        if (characteristics != null)
            foreach (var c in characteristics)
                Characteristics.Add(c);
    }

    [ObservableProperty]
    private string _UUID;

    [ObservableProperty]
    private bool _IsPrimary;

    [ObservableProperty]
    private ObservableCollection<BLECharacteristic> _Characteristics;

    public BLECharacteristic? GetCharacteristic(string id) => Characteristics?.FirstOrDefault(c => c.UUID.Equals(id, StringComparison.CurrentCultureIgnoreCase));
}

public partial class BLECharacteristic : ObservableObject
{
    public BLECharacteristic(string id, string prop, string? value = null)
    {
        UUID = id;
        Properties = prop;
        Value = value;
    }

    [ObservableProperty]
    private string _UUID;

    [ObservableProperty]
    private string? _Value;

    [ObservableProperty]
    private string _Properties;

    [ObservableProperty]
    private BLEPermissions _Permissions = BLEPermissions.Readable;

    public Action<string, string>? WriteRequest;
    public Func<string, string>? ReadRequest;
    public Action<string>? Subscribed;
    public Action<string>? Unsubscribed;
}