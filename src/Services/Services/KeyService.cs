namespace Turbo.Maui.Services;

public interface IKeyService
{
    string CurrentEnvironmentName { get; }
    string[] GetEnvironmentNames();
    void SetEnvironment(KeyService.DefaultEnvironments env);
    void SetEnvironment(string envName);
    void AddKeys(Dictionary<string, object> keys);
    void AddKey(string name, object value);
    bool RemoveKey(string name);
    T GetKey<T>(string name);
}

public class KeyService : IKeyService
{
    public KeyService()
    {
        //foreach (string env in Enum.GetNames(typeof(DefaultEnvironments)))
        //    SetEnvironment(env);
    }

    public void AddKey(string name, object value) => _CurrentEnvironment.AddKey(name, value);
    public void AddKeys(Dictionary<string, object> keys) => _CurrentEnvironment.AddKeys(keys);
    public bool RemoveKey(string name) => _CurrentEnvironment.RemoveKey(name);
    public T GetKey<T>(string name) => _CurrentEnvironment.GetKey<T>(name);
    public string[] GetEnvironmentNames() => _Environments.Select((x) => x.Environment).ToArray();

    public void SetEnvironment(DefaultEnvironments env) => SetEnvironment(Enum.GetName(env));
    public void SetEnvironment(string envName)
    {
        CurrentEnvironmentName = envName;
        var env = _Environments.FirstOrDefault((e) => e.Environment == envName);
        if (env == null) _Environments.Add(new KeyVault(envName));
    }

    public string CurrentEnvironmentName { get; private set; }

    public enum DefaultEnvironments { DEBUG, TEST, PRODUCTION }

    private KeyVault _CurrentEnvironment => _Environments.FirstOrDefault((e) => e.Environment == CurrentEnvironmentName);
    private List<KeyVault> _Environments = new List<KeyVault>();
}

public class KeyVault
{
    public KeyVault(string environment) { Environment = environment; }

    public void AddKey(string name, object value)
    {
        if (_Keys.ContainsKey(name))
            _Keys[name] = value;
        else
            _Keys.Add(name, value);
    }

    public void AddKeys(Dictionary<string, object> keys)
    {
        foreach (var key in keys)
            AddKey(key.Key, key.Value);
    }

    public bool RemoveKey(string name)
    {
        if (!_Keys.ContainsKey(name)) return false;

        _Keys.Remove(name);
        return true;
    }

    public T GetKey<T>(string name) => _Keys.ContainsKey(name) ? (T)_Keys[name] : throw new ArgumentOutOfRangeException($"Key: {name} not found.");

    public string Environment { get; private set; }
    private Dictionary<string, object> _Keys = new Dictionary<string, object>();
}