using System.Text.Json;
using YqlossKeyViewerDotNet.Utils;

namespace YqlossKeyViewerDotNet.Elements;

public class KeyElement : IElement
{
    public class StateUniqueData
    {
        public JsonColor BorderColor { get; set; } = default;
        public JsonColor InnerColor { get; set; }
        public List<TextElement> Texts { get; set; } = [];
    }

    public int KeyIndex { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double BorderWidth { get; set; } = 0;
    public double BorderHeight { get; set; } = 0;
    public double BorderRadius { get; set; } = 0;
    public double InnerRadius { get; set; } = 0;
    public StateUniqueData Released { get; set; } = SuppressUtil.LateInit<StateUniqueData>();
    public StateUniqueData Pressed { get; set; } = SuppressUtil.LateInit<StateUniqueData>();
    public List<TextElement> Texts { get; set; } = [];

    public void Draw(KeyViewer keyViewer)
    {
        var time = TimeUtil.TickTime();
        var scale = keyViewer.Config.Scale;
        var keyInfo = keyViewer.KeyManager.GetKeyInfo(KeyIndex);
        var keyData = keyViewer.KeyManager.GetKeyData(KeyIndex);
        var stateData = keyData.Pressed ? Pressed : Released;
        keyViewer.RenderEngine.DrawBorderedRect(
            X * scale,
            Y * scale,
            Width * scale,
            Height * scale,
            BorderWidth * scale,
            BorderHeight * scale,
            BorderRadius * scale,
            InnerRadius * scale,
            stateData.BorderColor,
            stateData.InnerColor
        );
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
        foreach (var text in Texts.Concat(stateData.Texts))
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