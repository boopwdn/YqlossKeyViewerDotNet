using System.Text.Json;
using YqlossKeyViewerDotNet.Elements;

namespace YqlossKeyViewerDotNet.Storages;

public class LayoutData
{
    public double Width { get; set; } = 640;
    public double Height { get; set; } = 480;
    public List<KeyInfo> Keys { get; set; } = [];
    public Dictionary<string, JsonElement> Templates { get; set; } = [];
    public List<JsonElement> Elements { get; set; } = [];

    public Dictionary<string, object> Extract(JsonElement json)
    {
        var result = new Dictionary<string, object>();

        if (json.TryGetProperty("Template", out var templateName))
        {
            var template = Templates[templateName.GetString()!];
            var extractedTemplate = Extract(template);
            result = result.Concat(extractedTemplate).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        foreach (var jsonProperty in json.EnumerateObject())
            result[jsonProperty.Name] = jsonProperty.Value.ValueKind switch
            {
                JsonValueKind.Object => Extract(jsonProperty.Value),
                JsonValueKind.Array => jsonProperty.Value
                    .EnumerateArray()
                    .Select<JsonElement, object>(it => it.ValueKind == JsonValueKind.Object ? Extract(it) : it)
                    .ToList(),
                _ => jsonProperty.Value
            };

        return result;
    }
}

public class Layout
{
    public required double Width { get; init; }
    public required double Height { get; init; }
    public required List<KeyInfo> Keys { get; init; }
    public required List<IElement> Elements { get; init; }

    public static Layout Read(string filePath)
    {
        var layoutData = JsonFile.Deserialize<LayoutData>(filePath)!;

        return new Layout
        {
            Width = layoutData.Width,
            Height = layoutData.Height,
            Keys = layoutData.Keys,
            Elements = (
                from element in layoutData.Elements
                let extracted = layoutData.Extract(element)
                select ElementRegistry.CreateElement(
                    JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(extracted))
                )
            ).ToList()
        };
    }
}