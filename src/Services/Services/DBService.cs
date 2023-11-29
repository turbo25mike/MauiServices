using System.Diagnostics;
using SQLite;

namespace Turbo.Maui.Services;

public interface IDBService : ITurboService
{
    //Task CreateTable<T>() where T : new();
    Task DeleteTable<T>() where T : new();
    Task Delete<T>(string id, bool sendToServer = true) where T : new();
    Task Insert<T>(T model, bool sendToServer = true) where T : new();
    Task Patch<T>(string id, object data, bool sendToServer = true) where T : new();
    Task Patch<T>(string id, IDictionary<string, object> data, bool sendToServer = true) where T : new();
    Task<T> Select<T>(int pk) where T : new();
    Task<T> Select<T>(string pk) where T : new();
    Task<List<T>> SelectAll<T>() where T : new();
    Task<List<T>> SelectAll<T>(string where) where T : new();
    Task<List<T>> Query<T>(string sql) where T : new();
    Task DeleteDB(params string[] explicitTables);
}
public class DBService : IDBService
{
    public DBService()
    {
        _DB = new SQLiteAsyncConnection(_ConnectionString, SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache);
    }

    public async Task DeleteDB(params string[] explicitTables)
    {
        var exclusions = "";
        if (explicitTables != null && explicitTables.Any())
            exclusions += $"AND name IN ({ConvertPropValueToSql(explicitTables)})";

        var tables = await _DB.QueryAsync<SQLTableName>($"SELECT name FROM sqlite_master WHERE type = 'table' {exclusions}");

        foreach (var table in tables)
            await _DB.ExecuteAsync($"DROP TABLE {table.name}");
    }

    public async Task DeleteTable<T>() where T : new() { if (await TableExists<T>()) await _DB.DropTableAsync<T>(); }

    public async Task Insert<T>(T model, bool sendToServer = true) where T : new()
    {
        Debug.WriteLine($"Inserting Record of type: {typeof(T).Name}");
        if (model == null) return;
        await CreateTable<T>();
        await _DB.InsertAsync(model);
    }
    public async Task Delete<T>(string id, bool sendToServer = true) where T : new()
    {
        var dbDeleteData = await Select<T>(id);
        if (dbDeleteData == null) return;

        if (await TableExists<T>()) await _DB.DeleteAsync(dbDeleteData);
    }

    public async Task Patch<T>(string id, object data, bool sendToServer = true) where T : new() => await Patch<T>(id, data.ToDictionary(), sendToServer);
    public async Task Patch<T>(string id, IDictionary<string, object> data, bool sendToServer = true) where T : new()
    {
        Debug.WriteLine($"Patch Record of type: {typeof(T).Name}");
        if (data == null) return;
        var dbData = await Select<T>(id);

        foreach (KeyValuePair<string, object> prop in data)
        {
            var dbProp = dbData.GetType().GetProperty(prop.Key);
            if (dbProp == null) continue;
            dbProp.SetValue(dbData, prop.Value);
        }

        if (await TableExists<T>()) await _DB.UpdateAsync(dbData);
    }

    public async Task<T> Select<T>(int pk) where T : new() => await QuerySingle<T>($"Select * from {typeof(T).Name} where ID = {pk}");

    public async Task<T> Select<T>(string pk) where T : new() => await QuerySingle<T>($"Select * from {typeof(T).Name} where ID = '{pk}'");

    public async Task<List<T>> SelectAll<T>() where T : new() => await Query<T>($"Select * from {typeof(T).Name}");

    public async Task<List<T>> SelectAll<T>(string where) where T : new() => await Query<T>($"Select * from {typeof(T).Name} where {where}");

    public async Task<List<T>> Query<T>(string sql) where T : new()
    {
        Debug.WriteLine($"Query type: {typeof(T).Name}");
        return await TableExists<T>() ? await _DB.QueryAsync<T>(sql) : null;
    }

    public async Task<T> QuerySingle<T>(string sql) where T : new()
    {
        var result = await Query<T>(sql);
        return (result != null) ? result.FirstOrDefault() : default;
    }

    #region Private Methods

    private async Task CreateTable<T>() where T : new()
    {
        if (!await TableExists<T>())
        {
            Debug.WriteLine($"Creating Table: {typeof(T).Name}");
            await _DB.CreateTableAsync<T>();
        }
    }

    private async Task<bool> TableExists<T>() where T : new()
    {
        try
        {
            Debug.WriteLine($"Table Check if Exists of type: {typeof(T).Name}");
            var results = await _DB.QueryAsync<T>($"SELECT name FROM sqlite_master WHERE type = 'table' AND name = '{typeof(T).Name}'");
            if (results != null && results.Count != 0)
            {
                await TableColumnCheck<T>();
                return true;
            }
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task<bool> TableColumnCheck<T>() where T : new()
    {
        try
        {
            var results = await _DB.GetTableInfoAsync(typeof(T).Name);

            foreach (var p in typeof(T).GetProperties())
            {
                var found = results.FirstOrDefault(t => t.Name == p.Name);
                if (found == null)
                {
                    if (p.CustomAttributes == null || !p.CustomAttributes.Any())
                    {
                        var colType = "TEXT";
                        if (p.PropertyType == typeof(int) || p.PropertyType == typeof(short))
                            colType = "INTEGER";
                        if (p.PropertyType == typeof(double) || p.PropertyType == typeof(float))
                            colType = "REAL";
                        if (p.PropertyType == typeof(decimal) || p.PropertyType == typeof(bool) || p.PropertyType == typeof(DateTime))
                            colType = "NUMERIC";
                        await _DB.ExecuteAsync($"ALTER TABLE {typeof(T).Name} ADD COLUMN {p.Name} {colType}");
                    }
                }
            }

            return results != null && results.Count != 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static object ConvertPropValueToSql(object val)
    {
        if (val == null) return "NULL";
        if (val is int || val is long || val is decimal) return val;
        if (val is bool v) return v ? 1 : 0;
        if (val is Array array)
        {
            var items = new List<string>();
            foreach (var o in array)
                items.Add(ConvertPropValueToSql(o).ToString());
            return string.Join(",", items);
        }
        if (val is DateTime time) return $"'{time.ToUniversalTime():yyyy-MM-dd HH:mm:ss}'";
        return $"'{val}'";
    }

    #endregion

    readonly SQLiteAsyncConnection _DB;
    private string _ConnectionString => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "App.db3");
}

