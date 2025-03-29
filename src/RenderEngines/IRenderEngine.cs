using YqlossKeyViewerDotNet.Elements;

namespace YqlossKeyViewerDotNet.RenderEngines;

public interface IRenderEngine
{
    void BeginDraw();

    void EndDraw();

    void SetClip(
        double x,
        double y,
        double width,
        double height
    );

    void ResetClip();

    void DrawBorderedRect(
        double x,
        double y,
        double width,
        double height,
        double borderWidth,
        double borderHeight,
        double borderRadius,
        double innerRadius,
        JsonColor borderColor,
        JsonColor innerColor
    );

    void DrawFadeBorderedRect(
        double x,
        double y,
        double width,
        double height,
        double borderWidth,
        double borderHeight,
        double borderRadius,
        double innerRadius,
        JsonColor borderColor,
        JsonColor innerColor,
        double fadeBeginX,
        double fadeBeginY,
        double fadeEndX,
        double fadeEndY
    );

    void DrawImage(
        string imagePath,
        double x,
        double y,
        double width,
        double height,
        double radius,
        double alpha
    );

    void DrawText(
        string text,
        double x,
        double y,
        double width,
        double height,
        double anchorX,
        double anchorY,
        string fontName,
        bool bold,
        bool italic,
        double fontSize,
        JsonColor color,
        double maxFontSize,
        bool shadow,
        double shadowX,
        double shadowY,
        JsonColor shadowColor
    );
}