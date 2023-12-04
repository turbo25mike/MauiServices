using System.Text.Json;
using System.Text.Json.Serialization;
namespace Turbo.Maui.Services.Models;


public class PatchDocument
{
    private List<Patch> _Patches = new();

    public PatchDocument(IEnumerable<Patch> patches) => _Patches.AddRange(patches);
    public PatchDocument() { }

    public void Add(Patch patch) => _Patches.Add(patch);
    public int Count() => _Patches.Count();
    public string Serialize() => JsonSerializer.Serialize(_Patches);
}

public enum PatchOp { add, remove, replace, move, copy, test }

public class Patch
{
    public Patch() { }

    public Patch(string prop, object propVal, PatchOp operation = PatchOp.replace)
    {
        Path = $"/" + prop.TrimStart('/');
        Op = operation;
        Value = propVal;
    }

    [JsonPropertyName("path")]
    public string Path { get; set; } = "";
    [JsonPropertyName("op")]
    public PatchOp Op { get; set; } = PatchOp.replace;
    [JsonPropertyName("value")]
    public object Value { get; set; } = new();
}