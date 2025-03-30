using System.Text.Json;
using YqlossKeyViewerDotNet.Utils;

namespace YqlossKeyViewerDotNet.Elements;

public class KeyElement : IElement
{
    public int KeyIndex { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public RectElement ReleasedRect { get; set; } = SuppressUtil.LateInit<RectElement>();
    public RectElement PressedRect { get; set; } = SuppressUtil.LateInit<RectElement>();
    public List<TextElement> Texts { get; set; } = [];
    public List<TextElement> ReleasedTexts { get; set; } = [];
    public List<TextElement> PressedTexts { get; set; } = [];

    public void Draw(KeyViewer keyViewer)
    {
        var time = TimeUtil.TickTime();
        var keyInfo = keyViewer.KeyManager.GetKeyInfo(KeyIndex);
        var keyData = keyViewer.KeyManager.GetKeyData(KeyIndex);
        var rect = keyData.Pressed ? PressedRect : ReleasedRect;
        var texts = keyData.Pressed ? PressedTexts : ReleasedTexts;

        {
            var originalX = rect.X;
            var originalY = rect.Y;
            var originalWidth = rect.Width;
            var originalHeight = rect.Height;
            rect.X += X;
            rect.Y += Y;
            rect.Width += Width;
            rect.Height += Height;
            rect.Draw(keyViewer);
            rect.X = originalX;
            rect.Y = originalY;
            rect.Width = originalWidth;
            rect.Height = originalHeight;
        }
        
        var placeholders = new Dictionary<string, object>
        {
            { "name", keyInfo.Name },
            { "counter", keyViewer.KeyCounter.GetCounter(keyInfo.Code) },
            {
                "kps",
                keyData.RainsForRender
                    .Select(it => it.From)
                    .Let(it => keyData.Pressed ? it.Append(keyData.PressTime) : it)
                    .Count(it => TimeUtil.TickToNano(time - it) < 1e9)
            }
        };

        foreach (var text in Texts.Concat(texts))
        {
            var originalX = text.X;
            var originalY = text.Y;
            var originalWidth = text.Width;
            var originalHeight = text.Height;
            var originalText = text.Text;
            text.X += X;
            text.Y += Y;
            text.Width += Width;
            text.Height += Height;
            text.Text = PlaceholderUtil.Format(text.Text, placeholders);
            text.Draw(keyViewer);
            text.X = originalX;
            text.Y = originalY;
            text.Width = originalWidth;
            text.Height = originalHeight;
            text.Text = originalText;
        }
    }

    public static KeyElement Parse(JsonElement json)
    {
        return json.Deserialize<KeyElement>()!;
    }
}