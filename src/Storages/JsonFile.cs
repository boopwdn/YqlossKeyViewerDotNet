using System.Text;
using System.Text.Json;

namespace YqlossKeyViewerDotNet.Storages;

public static class JsonFile
{
    public static T? Deserialize<T>(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath, Encoding.UTF8);
            return JsonSerializer.Deserialize<T>(json) ?? default;
        }
        catch
        {
            return default;
        }
    }

    public static void Serialize<T>(string filePath, T value)
    {
        File.WriteAllText(filePath, JsonSerializer.Serialize(value));
    }
}