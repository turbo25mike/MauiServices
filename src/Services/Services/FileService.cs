namespace Turbo.Maui.Services;

public interface IFileService
{
    Task<byte[]> ReadAllBytes(string fileName);
    Task<string> ReadAllText(string fileName);
    bool FileExists(string fileName);
    void SaveFile(byte[] bytes, string fileName, FileMode mode = FileMode.OpenOrCreate);
    void DeleteFile(string fileName);
    DateTime GetFileCreationTimeUtc(string fileName);
    IEnumerable<string> GetDirectories(string path);
    IEnumerable<string> GetFiles(string path);
}

public class FileService : IFileService
{
    public async Task<byte[]> ReadAllBytes(string fileName) => await File.ReadAllBytesAsync(Path.Combine(FileSystem.Current.AppDataDirectory, fileName));
    public async Task<string> ReadAllText(string fileName) => await File.ReadAllTextAsync(Path.Combine(FileSystem.Current.AppDataDirectory, fileName));

    public bool FileExists(string fileName) => File.Exists(Path.Combine(FileSystem.Current.AppDataDirectory, fileName));

    public IEnumerable<string> GetFiles(string path) => Directory.EnumerateFiles(Path.Combine(FileSystem.Current.AppDataDirectory, path));
    public IEnumerable<string> GetDirectories(string path) => Directory.EnumerateDirectories(Path.Combine(FileSystem.Current.AppDataDirectory, path));

    public DateTime GetFileCreationTimeUtc(string fileName) => File.GetCreationTimeUtc(Path.Combine(FileSystem.Current.AppDataDirectory, fileName));

    public void SaveFile(byte[] bytes, string fileName, FileMode mode = FileMode.OpenOrCreate)
    {
        var directory = fileName[..(fileName.LastIndexOf('/') + 1)];
        var file = fileName[(fileName.LastIndexOf('/') + 1)..];
        var filePath = Path.Combine(FileSystem.Current.AppDataDirectory, directory);

        Directory.CreateDirectory(filePath);

        using var fileStream = new FileStream(Path.Combine(filePath, file), mode);
        for (var i = 0; i < bytes.Length; i++)
            fileStream.WriteByte(bytes[i]);

        fileStream.Close();
    }

    public void DeleteFile(string fileName)
    {
        if (!FileExists(fileName)) return;
        var path = Path.Combine(FileSystem.Current.AppDataDirectory, fileName);
        File.Delete(path);
    }

    //Used only outside of app dir
    //Requires WRITE EXTERNAL STORAGE permissions
    //if (await Permissions.CheckStatusAsync<Permissions.StorageWrite>() != PermissionStatus.Granted)
    //    if (await Permissions.RequestAsync<Permissions.StorageWrite>() != PermissionStatus.Granted)
    //        throw new ArgumentException("Permissions are required.");
}

