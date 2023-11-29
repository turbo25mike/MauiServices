namespace Turbo.Maui.Services.Tests.TestExtentions;

public class Then
{
    public int SomeFunction(string name) => !_Functions.ContainsKey(name) ? 0 : _Functions[name];

    public void AddCall(string name)
    {
        if (!_Functions.ContainsKey(name))
            _Functions.Add(name, 1);
        else
            _Functions[name]++;
    }

    private readonly Dictionary<string, int> _Functions = new();
}

public static class ThenExt
{
    public static bool WasCalled(this int calls) => calls > 0;

    public static bool WasCalledOnce(this int calls) => calls == 1;
}