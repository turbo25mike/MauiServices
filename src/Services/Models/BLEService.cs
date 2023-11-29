using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Turbo.Maui.Services.Models;

public partial class BLEService : ObservableObject
{
    public BLEService(string id, IEnumerable<BLECharacteristic> characteristics)
    {
        UUID = id;
        Characteristics = new();
        if (characteristics != null)
            foreach (var c in characteristics)
                Characteristics.Add(c);
    }

    [ObservableProperty]
    private string _UUID;

    [ObservableProperty]
    private ObservableCollection<BLECharacteristic> _Characteristics;
}

public partial class BLECharacteristic : ObservableObject
{
    public BLECharacteristic(string id, string prop)
    {
        UUID = id;
        Properties = prop;
    }

    [ObservableProperty]
    private string _UUID;

    [ObservableProperty]
    private string _Properties;
}