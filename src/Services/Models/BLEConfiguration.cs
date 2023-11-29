namespace Turbo.Maui.Services.Models;

public class BLEConfiguration
{
    public string Service { get; set; }
    public string ReadAddress { get; set; }
    public string WriteAddress { get; set; }
    public BLERegister KeepAlive { get; set; }
}

public class BLERegister
{
    public int Register { get; set; }
    public uint Value { get; set; }
}