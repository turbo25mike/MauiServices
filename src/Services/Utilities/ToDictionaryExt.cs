using System.ComponentModel;

namespace Turbo.Maui.Services.Utilities;

internal static class ToDictionaryExtension
{
    internal static IDictionary<string, object>[] ToDictionary(this IEnumerable<object> source)
    {
        return source.Select(s => s.ToDictionary()).ToArray();
    }

    internal static IDictionary<string, object> ToDictionary(this object source)
    {
        return source.ToDictionary<object>();
    }

    internal static IDictionary<string, T> ToDictionary<T>(this object source)
    {
        if (source == null)
            throw new ArgumentNullException("source", "Unable to convert object to a dictionary. The source object is null.");

        var dictionary = new Dictionary<string, T>();
        foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(source))
            AddPropertyToDictionary(property, source, dictionary);

        return dictionary;
    }

    internal static void AddPropertyToDictionary(this IDictionary<string, object> dict, string propName, object propVal)
    {
        if (!dict.ContainsKey(propName)) dict.Add(new KeyValuePair<string, object>(propName, propVal));
        dict[propName] = propVal;
    }

    private static void AddPropertyToDictionary<T>(PropertyDescriptor property, object source, Dictionary<string, T> dictionary)
    {
        var value = property.GetValue(source);
        if (value is not null and T)
            dictionary.Add(property.Name, (T)value);
    }
}

