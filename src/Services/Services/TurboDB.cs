using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Turbo.Maui.Services;

public static class TurboDB
{
    public static IEnumerable<string> GetTables() => Directory.EnumerateFiles(DB_FOLDER_PATH);

    public static void DeleteTable<T>()
    {
        if (!TableExists<T>()) return;
        var path = TablePath<T>();
        File.Delete(path);
    }

    public static void Insert<T>(T model) where T : new()
    {
        var data = ReadTable<T>();
        data.Add(model);
        SaveTable(data);
    }

    public static void Delete<T>(Predicate<T> match) where T : new()
    {
        var tableData = ReadTable<T>();
        var found = tableData.Find(match);
        if (found == null) return;
        tableData.Remove(found);
        SaveTable(tableData);
    }

    public static void Patch<T>(Predicate<T> match, object data) where T : new()
    {
        if (data is null) return;
        Patch(match, data.ToDictionary());
    }

    public static void Patch<T>(Predicate<T> match, IDictionary<string, object> data) where T : new()
    {
        if (data == null) return;

        var tableData = ReadTable<T>();
        var found = tableData.Find(match);
        if (found == null) return;

        foreach (KeyValuePair<string, object> prop in data)
        {
            var dbProp = found.GetType().GetProperty(prop.Key);
            if (dbProp == null) continue;
            dbProp.SetValue(found, prop.Value);
        }

        SaveTable(tableData);
    }

    public static T? Select<T>(Predicate<T> match) where T : new()
    {
        var tableData = ReadTable<T>();
        return tableData.Find(match);
    }

    public static List<T> SelectAll<T>() where T : new() => ReadTable<T>();

    public static List<T> SelectAll<T>(Predicate<T> match) where T : new()
    {
        var tableData = ReadTable<T>();
        return tableData.FindAll(match);
    }

    #region Private Methods

    private static List<T> ReadTable<T>()
    {
        if (!TableExists<T>()) return new();
        var path = TablePath<T>();
        var bytes = File.ReadAllBytes(path);
        var data = Encoding.ASCII.GetString(Decompress(bytes));
        var results = JsonSerializer.Deserialize<List<T>>(data ?? "");
        return results ?? new();
    }

    private static bool TableExists<T>() => File.Exists(TablePath<T>());

    private static void SaveTable<T>(List<T> data)
    {
        Directory.CreateDirectory(DB_FOLDER_PATH);
        var bytes = Compress(Encoding.ASCII.GetBytes(JsonSerializer.Serialize(data)));

        using var fileStream = new FileStream(TablePath<T>(), FileMode.Create);

        fileStream.Write(bytes, 0, bytes.Length);
    }

    private static byte[] Compress(byte[] data)
    {
        using var compressedStream = new MemoryStream();
        using (var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal, false))
        {
            gzipStream.Write(data);
        }

        return compressedStream.ToArray();
    }

    private static byte[] Decompress(byte[] compressedData)
    {
        using var uncompressedStream = new MemoryStream();

        using (var compressedStream = new MemoryStream(compressedData))
        using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress, false))
        {
            gzipStream.CopyTo(uncompressedStream);
        }

        return uncompressedStream.ToArray();
    }

    private static string TablePath<T>() => Path.Combine(DB_FOLDER_PATH, $"{typeof(T).Name}.db");

    private static string DB_FOLDER_PATH => Path.Combine(FileSystem.AppDataDirectory, "TurboDB");
    #endregion
}