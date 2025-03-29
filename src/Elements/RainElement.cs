using System.Text.Json;
using YqlossKeyViewerDotNet.Utils;

namespace YqlossKeyViewerDotNet.Elements;

public class RainElement : IElement
{
    public int KeyIndex { get; set; }
    public double Speed { get; set; } = 4e6;
    public bool FlowUp { get; set; } = true;
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
    public bool FadeOut { get; set; } = false;
    public double FadeBeginX { get; set; }
    public double FadeBeginY { get; set; }
    public double FadeEndX { get; set; }
    public double FadeEndY { get; set; }
    public double DFromTime { get; set; } = 0;
    public double DTillTime { get; set; } = 0;

    private void DrawRain(KeyViewer keyViewer, long time, long from, long till)
    {
        var sinceFrom = TimeUtil.TickToNano(time - from) - DFromTime;
        var sinceTill = TimeUtil.TickToNano(time - till) - DTillTime;
        var scale = keyViewer.Config.Scale;
        var yFrom = sinceFrom / Speed;
        var yTill = Math.Min(sinceTill / Speed, Height);
        if (Math.Min(yFrom, Height) <= yTill) return;
        var yPos = FlowUp ? Y + Height - yFrom : Y + yTill;
        if (FadeOut)
            keyViewer.RenderEngine.DrawFadeBorderedRect(
                X * scale,
                yPos * scale,
                Width * scale,
                (yFrom - yTill) * scale,
                BorderWidth * scale,
                BorderHeight * scale,
                BorderRadius * scale,
                InnerRadius * scale,
                BorderColor,
                InnerColor,
                FadeBeginX * scale,
                FadeBeginY * scale,
                FadeEndX * scale,
                FadeEndY * scale
            );
        else
            keyViewer.RenderEngine.DrawBorderedRect(
                X * scale,
                yPos * scale,
                Width * scale,
                (yFrom - yTill) * scale,
                BorderWidth * scale,
                BorderHeight * scale,
                BorderRadius * scale,
                InnerRadius * scale,
                BorderColor,
                InnerColor
            );
    }

    public void Draw(KeyViewer keyViewer)
    {
        var scale = keyViewer.Config.Scale;
        var time = TimeUtil.TickTime();
        var keyData = keyViewer.KeyManager.GetKeyData(KeyIndex);
        keyViewer.RenderEngine.SetClip(X * scale, Y * scale, Width * scale, Height * scale);
        foreach (var rain in keyData.RainsForRender) DrawRain(keyViewer, time, rain.From, rain.Till);
        if (keyData.Pressed) DrawRain(keyViewer, time, keyData.PressTime, time);
        keyViewer.RenderEngine.ResetClip();
    }

    public static RainElement Parse(JsonElement json)
    {
        return json.Deserialize<RainElement>()!;
    }
}