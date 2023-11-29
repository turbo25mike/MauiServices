using System.ComponentModel;

namespace Turbo.Maui.Services.Utilities;

public static class ToDictionaryExtension
{
    public static IDictionary<string, object>[] ToDictionary(this IEnumerable<object> source)
    {
        return source.Select(s => s.ToDictionary()).ToArray();
    }

    public static IDictionary<string, object> ToDictionary(this object source)
    {
        return source.ToDictionary<object>();
    }

    public static IDictionary<string, T> ToDictionary<T>(this object source)
    {
        if (source == null)
            ThrowExceptionWhenSourceArgumentIsNull();

        var dictionary = new Dictionary<string, T>();
        foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(source))
            AddPropertyToDictionary(property, source, dictionary);

        return dictionary;
    }

    public static void AddPropertyToDictionary(this IDictionary<string, object> dict, string propName, object propVal)
    {
        if (!dict.ContainsKey(propName)) dict.Add(new KeyValuePair<string, object>(propName, propVal));
        dict[propName] = propVal;
    }

    private static void AddPropertyToDictionary<T>(PropertyDescriptor property, object source,
        Dictionary<string, T> dictionary)
    {
        var value = property.GetValue(source);
        if (IsOfType<T>(value))
            dictionary.Add(property.Name, (T)value);
    }

    private static bool IsOfType<T>(object value)
    {
        return value is T;
    }

    private static void ThrowExceptionWhenSourceArgumentIsNull()
    {
        throw new ArgumentNullException("source",
            "Unable to convert object to a dictionary. The source object is null.");
    }
}

