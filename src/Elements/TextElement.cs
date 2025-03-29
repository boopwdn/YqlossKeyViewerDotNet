using System.Text.Json;
using YqlossKeyViewerDotNet.Utils;

namespace YqlossKeyViewerDotNet.Elements;

public class TextElement : IElement
{
    public string Text { get; set; } = SuppressUtil.LateInit<string>();
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double AnchorX { get; set; } = 0.5;
    public double AnchorY { get; set; } = 0.5;
    public string FontName { get; set; } = "";
    public bool Bold { get; set; } = false;
    public bool Italic { get; set; } = false;
    public double FontSize { get; set; }
    public JsonColor Color { get; set; }
    public double? MaxFontSize { get; set; } = null;
    public bool Shadow { get; set; } = false;
    public double ShadowX { get; set; }
    public double ShadowY { get; set; }
    public JsonColor ShadowColor { get; set; }

    public void Draw(KeyViewer keyViewer)
    {
        var scale = keyViewer.Config.Scale;
        keyViewer.RenderEngine.DrawText(
            Text,
            X * scale,
            Y * scale,
            Width * scale,
            Height * scale,
            AnchorX,
            AnchorY,
            FontName,
            Bold,
            Italic,
            FontSize * scale,
            Color,
            (MaxFontSize ?? double.PositiveInfinity) * scale,
            Shadow,
            ShadowX,
            ShadowY,
            ShadowColor
        );
    }

    public static TextElement Parse(JsonElement json)
    {
        return json.Deserialize<TextElement>()!;
    }
}