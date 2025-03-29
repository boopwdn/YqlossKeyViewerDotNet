namespace YqlossKeyViewerDotNet.Storages;

public class Storage
{
    public int WindowX { get; set; }
    public int WindowY { get; set; }

    public void Save(string filePath)
    {
        JsonFile.Serialize(filePath, this);
    }

    public static Storage ReadAndSave(string filePath)
    {
        var storage = File.Exists(filePath) ? JsonFile.Deserialize<Storage>(filePath)! : new Storage();
        storage.Save(filePath);
        return storage;
    }
}