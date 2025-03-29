using System.Text.Json;

namespace YqlossKeyViewerDotNet.Elements;

public static class ElementRegistry
{
    private delegate IElement ElementFactory(JsonElement json);

    static ElementRegistry()
    {
        Registry["Rect"] = RectElement.Parse;
        Registry["Image"] = ImageElement.Parse;
        Registry["Text"] = TextElement.Parse;
        Registry["Key"] = KeyElement.Parse;
        Registry["Rain"] = RainElement.Parse;
    }

    private static Dictionary<string, ElementFactory> Registry { get; } = [];

    public static IElement CreateElement(JsonElement json)
    {
        var type = json.GetProperty("Type").GetString()!;
        return Registry[type](json);
    }
}