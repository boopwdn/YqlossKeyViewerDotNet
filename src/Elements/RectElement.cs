using System.Text.Json;

namespace YqlossKeyViewerDotNet.Elements;

public class RectElement : IElement
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double BorderWidth { get; set; } = 0;
    public double BorderHeight { get; set; } = 0;
    public double BorderRadius { get; set; } = 0;
    public double InnerRadius { get; set; } = 0;
    public JsonColor BorderColor { get; set; } = default;
    public JsonColor InnerColor { get; set; }

    public void Draw(KeyViewer keyViewer)
    {
        var scale = keyViewer.Config.Scale;
        keyViewer.RenderEngine.DrawBorderedRect(
            X * scale,
            Y * scale,
            Width * scale,
            Height * scale,
            BorderWidth * scale,
            BorderHeight * scale,
            BorderRadius * scale,
            InnerRadius * scale,
            BorderColor,
            InnerColor
        );
    }

    public static RectElement Parse(JsonElement json)
    {
        return json.Deserialize<RectElement>()!;
    }
}