using System.Text.Json;
using YqlossKeyViewerDotNet.Utils;

namespace YqlossKeyViewerDotNet.Elements;

public class ImageElement : IElement
{
    public string Image { get; set; } = SuppressUtil.LateInit<string>();
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double Radius { get; set; } = 0;
    public double Alpha { get; set; } = 255;

    public void Draw(KeyViewer keyViewer)
    {
        var scale = keyViewer.Config.Scale;
        keyViewer.RenderEngine.DrawImage(
            Image,
            X * scale,
            Y * scale,
            Width * scale,
            Height * scale,
            Radius * scale,
            Alpha / 255
        );
    }

    public static ImageElement Parse(JsonElement json)
    {
        return json.Deserialize<ImageElement>()!;
    }
}