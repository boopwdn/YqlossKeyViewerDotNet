using System.Numerics;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDX.WIC;
using Vanara.PInvoke;
using YqlossKeyViewerDotNet.Elements;
using YqlossKeyViewerDotNet.Utils;
using YqlossKeyViewerDotNet.Windows;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using Bitmap = SharpDX.Direct2D1.Bitmap;
using BitmapInterpolationMode = SharpDX.Direct2D1.BitmapInterpolationMode;
using Factory = SharpDX.Direct2D1.Factory;
using FactoryType = SharpDX.Direct2D1.FactoryType;
using PixelFormat = SharpDX.Direct2D1.PixelFormat;
using TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode;

namespace YqlossKeyViewerDotNet.RenderEngines;

public class Direct2DRenderEngine : IRenderEngine
{
    private static Gdi32.BLENDFUNCTION BlendFunction { get; } = new()
    {
        AlphaFormat = 1, // AC_SRC_ALPHA
        BlendFlags = 0,
        BlendOp = 0, // AC_SRC_OVER
        SourceConstantAlpha = 255
    };

    private static POINT Origin { get; } = new(0, 0);
    private SIZE ClientSize { get; set; }

    public Direct2DRenderEngine(Window window)
    {
        HWnd = window.HWnd;

        var renderTargetProperties = new RenderTargetProperties
        {
            Type = RenderTargetType.Default,
            PixelFormat = new PixelFormat(Format.B8G8R8A8_UNorm, AlphaMode.Premultiplied),
            DpiX = 0,
            DpiY = 0,
            MinLevel = FeatureLevel.Level_DEFAULT,
            Usage = RenderTargetUsage.None
        };

        RenderTarget = new DeviceContextRenderTarget(Factory, renderTargetProperties);
        RenderTarget.AntialiasMode = AntialiasMode.PerPrimitive;
        RenderTarget.TextAntialiasMode = TextAntialiasMode.Cleartype;

        ColorBrushMap = new DefaultDictionary<JsonColor, (byte, byte, byte, byte), SolidColorBrush>
        {
            KeyTransformer = jsonColor => (jsonColor.R, jsonColor.G, jsonColor.B, jsonColor.A),
            DefaultValue = (jsonColor, _) => new SolidColorBrush(RenderTarget, ToD2DColor(jsonColor))
        };

        BitmapMap = new DefaultDictionary<string, string, Bitmap>
        {
            KeyTransformer = it => it,
            DefaultValue = (imagePath, _) =>
            {
                using var decoder = new BitmapDecoder(ImagingFactory, imagePath, DecodeOptions.CacheOnDemand);
                using var frame = decoder.GetFrame(0);
                using var formatConverter = new FormatConverter(ImagingFactory);
                formatConverter.Initialize(frame, SharpDX.WIC.PixelFormat.Format32bppPRGBA);
                return Bitmap.FromWicBitmap(RenderTarget, formatConverter);
            }
        };

        BitmapBrushMap = new DefaultDictionary<
            (string, double, double, double),
            (string, double, double, double),
            BitmapBrush
        >
        {
            KeyTransformer = it => it,
            DefaultValue = (key, _) =>
            {
                var (imagePath, width, height, alpha) = key;
                var bitmap = BitmapMap[imagePath];
                return new BitmapBrush(
                    RenderTarget,
                    bitmap,
                    new BitmapBrushProperties
                    {
                        ExtendModeX = ExtendMode.Mirror,
                        ExtendModeY = ExtendMode.Mirror,
                        InterpolationMode = BitmapInterpolationMode.Linear
                    },
                    new BrushProperties
                    {
                        Opacity = (float)alpha,
                        Transform = new RawMatrix3x2(
                            (float)width / bitmap.Size.Width, 0,
                            0, (float)height / bitmap.Size.Height,
                            0, 0
                        )
                    }
                );
            }
        };

        GradientBrushMap = new DefaultDictionary<
            (JsonColor, double, double, double, double),
            (byte, byte, byte, byte, double, double, double, double),
            LinearGradientBrush
        >
        {
            KeyTransformer = key =>
            {
                var (jsonColor, bx, by, ex, ey) = key;
                return (jsonColor.R, jsonColor.G, jsonColor.B, jsonColor.A, bx, by, ex, ey);
            },
            DefaultValue = (key, _) =>
            {
                var (jsonColor, bx, by, ex, ey) = key;
                var collection = new GradientStopCollection(
                    RenderTarget,
                    [
                        new GradientStop { Position = 0, Color = ToD2DColor(jsonColor, a: 0) },
                        new GradientStop { Position = 1, Color = ToD2DColor(jsonColor) }
                    ],
                    Gamma.StandardRgb
                );
                return new LinearGradientBrush(
                    RenderTarget,
                    new LinearGradientBrushProperties
                    {
                        StartPoint = new RawVector2((float)bx, (float)by),
                        EndPoint = new RawVector2((float)ex, (float)ey)
                    },
                    collection
                );
            }
        };

        TextFormatMap = new DefaultDictionary<
            (string, bool, bool, double),
            (string, bool, bool, double),
            TextFormat
        >
        {
            KeyTransformer = it => it,
            DefaultValue = (key, _) =>
            {
                var (fontName, bold, italic, fontSize) = key;
                return new TextFormat(
                    DirectWriteFactory,
                    fontName,
                    bold ? FontWeight.Bold : FontWeight.Regular,
                    italic ? FontStyle.Italic : FontStyle.Normal,
                    (float)fontSize
                );
            }
        };
    }

    private User32.SafeHWND HWnd { get; }
    private User32.SafeReleaseHDC HDc { get; set; } = SuppressUtil.LateInit<User32.SafeReleaseHDC>();
    private bool CDcInitialized { get; set; }
    private Gdi32.SafeHDC CDc { get; set; } = SuppressUtil.LateInit<Gdi32.SafeHDC>();
    private Gdi32.SafeHBITMAP HBitmap { get; set; } = SuppressUtil.LateInit<Gdi32.SafeHBITMAP>();
    private Factory Factory { get; } = new(FactoryType.SingleThreaded);
    private SharpDX.DirectWrite.Factory DirectWriteFactory { get; } = new(SharpDX.DirectWrite.FactoryType.Shared);
    private ImagingFactory ImagingFactory { get; } = new();
    private DeviceContextRenderTarget RenderTarget { get; }

    private bool Clipped { get; set; }
    private DefaultDictionary<JsonColor, (byte, byte, byte, byte), SolidColorBrush> ColorBrushMap { get; }
    private DefaultDictionary<string, string, Bitmap> BitmapMap { get; }

    private DefaultDictionary<
        (string, double, double, double),
        (string, double, double, double),
        BitmapBrush
    > BitmapBrushMap { get; }

    private DefaultDictionary<
        (JsonColor, double, double, double, double),
        (byte, byte, byte, byte, double, double, double, double),
        LinearGradientBrush
    > GradientBrushMap { get; }

    private DefaultDictionary<
        (string, bool, bool, double),
        (string, bool, bool, double),
        TextFormat
    > TextFormatMap { get; }

    public void BeginDraw()
    {
        HDc = User32.GetDC(HWnd);

        if (!CDcInitialized)
        {
            User32.GetClientRect(HWnd, out var clientRect);
            ClientSize = new SIZE(clientRect.Width, clientRect.Height);
            CDc = Gdi32.CreateCompatibleDC(HDc);
            HBitmap = Gdi32.CreateCompatibleBitmap(HDc, clientRect.Width, clientRect.Height);
            Gdi32.DeleteObject(Gdi32.SelectObject(CDc, HBitmap));
            RenderTarget.BindDeviceContext(
                CDc.DangerousGetHandle(),
                new RawRectangle(0, 0, clientRect.Width, clientRect.Height)
            );
            RenderTarget.AntialiasMode = AntialiasMode.PerPrimitive;
            RenderTarget.TextAntialiasMode = TextAntialiasMode.Grayscale;
            CDcInitialized = true;
        }

        RenderTarget.BeginDraw();
        RenderTarget.Clear(new RawColor4(0, 0, 0, 0));
    }

    public void EndDraw()
    {
        RenderTarget.EndDraw();
        User32.UpdateLayeredWindow(
            HWnd,
            HDc,
            new POINT(KeyViewer.Instance.Storage.WindowX, KeyViewer.Instance.Storage.WindowY),
            ClientSize,
            CDc,
            Origin,
            0,
            BlendFunction,
            User32.UpdateLayeredWindowFlags.ULW_ALPHA
        );
        HDc.Dispose();
    }

    public void SetClip(
        double x,
        double y,
        double width,
        double height
    )
    {
        if (Clipped) RenderTarget.PopAxisAlignedClip();
        RenderTarget.PushAxisAlignedClip(
            new RawRectangleF(
                (float)x,
                (float)y,
                (float)(x + width),
                (float)(y + height)
            ),
            AntialiasMode.PerPrimitive
        );
        Clipped = true;
    }

    public void ResetClip()
    {
        if (Clipped) RenderTarget.PopAxisAlignedClip();
        Clipped = false;
    }

    private void DrawBorderedRectBrush(
        double x,
        double y,
        double width,
        double height,
        double borderWidth,
        double borderHeight,
        double borderRadius,
        double innerRadius,
        Brush brushBorder,
        Brush brushInner
    )
    {
        var borderRect = new RawRectangleF(
            (float)x,
            (float)y,
            (float)(x + width),
            (float)(y + height)
        );

        var actualBorderWidth = (float)Math.Min(borderWidth, width / 2);
        var actualBorderHeight = (float)Math.Min(borderHeight, height / 2);
        var actualBorderRadius = (float)Math.Min(borderRadius, Math.Min(width / 2, height / 2));

        var borderRoundedRect = new RoundedRectangle
        {
            Rect = borderRect,
            RadiusX = actualBorderRadius,
            RadiusY = actualBorderRadius
        };

        RenderTarget.FillRoundedRectangle(borderRoundedRect, brushInner);

        if (actualBorderWidth <= 0.0 && actualBorderHeight <= 0.0) return;

        var innerRect = new RawRectangleF(
            borderRect.Left + actualBorderWidth,
            borderRect.Top + actualBorderHeight,
            borderRect.Right - actualBorderWidth,
            borderRect.Bottom - actualBorderHeight
        );

        var innerWidth = width - actualBorderWidth * 2;
        var innerHeight = height - actualBorderHeight * 2;
        var actualInnerRadius = (float)Math.Min(innerRadius, Math.Min(innerWidth / 2, innerHeight / 2));
        var innerRoundedRect = new RoundedRectangle
        {
            Rect = innerRect,
            RadiusX = actualInnerRadius,
            RadiusY = actualInnerRadius
        };

        using var geometry = new PathGeometry(Factory);

        {
            using var sink = geometry.Open();
            sink.AddRoundedRectangle(FigureBegin.Filled, borderRoundedRect);
            sink.AddRoundedRectangle(FigureBegin.Filled, innerRoundedRect, true);
            sink.Close();
        }

        RenderTarget.FillGeometry(geometry, brushBorder);
    }

    public void DrawBorderedRect(
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
    )
    {
        DrawBorderedRectBrush(
            x,
            y,
            width,
            height,
            borderWidth,
            borderHeight,
            borderRadius,
            innerRadius,
            ColorBrushMap[borderColor],
            ColorBrushMap[innerColor]
        );
    }

    public void DrawFadeBorderedRect(
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
    )
    {
        DrawBorderedRectBrush(
            x,
            y,
            width,
            height,
            borderWidth,
            borderHeight,
            borderRadius,
            innerRadius,
            GradientBrushMap[(borderColor, fadeBeginX, fadeBeginY, fadeEndX, fadeEndY)],
            GradientBrushMap[(innerColor, fadeBeginX, fadeBeginY, fadeEndX, fadeEndY)]
        );
    }

    public void DrawImage(
        string imagePath,
        double x,
        double y,
        double width,
        double height,
        double radius,
        double alpha
    )
    {
        var borderRect = new RawRectangleF(
            0,
            0,
            (float)width,
            (float)height
        );

        var actualRadius = (float)Math.Min(radius, Math.Min(width / 2, height / 2));

        var borderRoundedRect = new RoundedRectangle
        {
            Rect = borderRect,
            RadiusX = actualRadius,
            RadiusY = actualRadius
        };

        var rawMatrix = RenderTarget.Transform;
        RenderTarget.Transform = ToRawMatrix(ToMatrix(rawMatrix) * new Matrix3x2(1, 0, 0, 1, (float)x, (float)y));
        RenderTarget.FillRoundedRectangle(borderRoundedRect, BitmapBrushMap[(imagePath, width, height, alpha)]);
        RenderTarget.Transform = rawMatrix;
    }

    public void DrawText(
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
    )
    {
        var font = TextFormatMap[(fontName, bold, italic, fontSize)];
        using var textLayout =
            new TextLayout(DirectWriteFactory, text, font, float.PositiveInfinity, float.PositiveInfinity);
        var metrics = textLayout.Metrics;
        var size = (float)Math.Min(maxFontSize / fontSize, Math.Min(width / metrics.Width, height / metrics.Height));
        var xPos = x + (width - metrics.Width * size) * anchorX;
        var yPos = y + (height - metrics.Height * size) * anchorY;
        var rawMatrix = RenderTarget.Transform;
        RenderTarget.Transform = ToRawMatrix(
            ToMatrix(rawMatrix) * new Matrix3x2(size, 0, 0, size, (float)xPos, (float)yPos)
        );
        if (shadow)
            RenderTarget.DrawTextLayout(
                new RawVector2((float)shadowX, (float)shadowY),
                textLayout,
                ColorBrushMap[shadowColor]
            );
        RenderTarget.DrawTextLayout(new RawVector2(0, 0), textLayout, ColorBrushMap[color]);
        RenderTarget.Transform = rawMatrix;
    }

    private static RawColor4 ToD2DColor(
        JsonColor jsonColor,
        float? r = null,
        float? g = null,
        float? b = null,
        float? a = null
    )
    {
        return new RawColor4(
            r ?? jsonColor.R / 255.0F,
            g ?? jsonColor.G / 255.0F,
            b ?? jsonColor.B / 255.0F,
            a ?? jsonColor.A / 255.0F
        );
    }

    private static Matrix3x2 ToMatrix(RawMatrix3x2 rawMatrix)
    {
        return new Matrix3x2(
            rawMatrix.M11,
            rawMatrix.M12,
            rawMatrix.M21,
            rawMatrix.M22,
            rawMatrix.M31,
            rawMatrix.M32
        );
    }

    private static RawMatrix3x2 ToRawMatrix(Matrix3x2 rawMatrix)
    {
        return new RawMatrix3x2(
            rawMatrix.M11,
            rawMatrix.M12,
            rawMatrix.M21,
            rawMatrix.M22,
            rawMatrix.M31,
            rawMatrix.M32
        );
    }
}

public static class Direct2DExtensions
{
    public static void AddRoundedRectangle(
        this GeometrySink sink,
        FigureBegin figureBegin,
        RoundedRectangle rect,
        bool counterClockWise = false
    )
    {
        var drawArc = rect.RadiusX > 0.0 && rect.RadiusY > 0.0;
        var p0 = new RawVector2(rect.Rect.Left + rect.RadiusX, rect.Rect.Top);
        var p1 = new RawVector2(rect.Rect.Right - rect.RadiusX, rect.Rect.Top);
        var p2 = new RawVector2(rect.Rect.Right, rect.Rect.Top + rect.RadiusY);
        var p3 = new RawVector2(rect.Rect.Right, rect.Rect.Bottom - rect.RadiusY);
        var p4 = new RawVector2(rect.Rect.Right - rect.RadiusX, rect.Rect.Bottom);
        var p5 = new RawVector2(rect.Rect.Left + rect.RadiusX, rect.Rect.Bottom);
        var p6 = new RawVector2(rect.Rect.Left, rect.Rect.Bottom - rect.RadiusY);
        var p7 = new RawVector2(rect.Rect.Left, rect.Rect.Top + rect.RadiusY);

        if (counterClockWise)
        {
            sink.BeginFigure(p0, figureBegin);
            if (drawArc) sink.AddArc(RoundedCornerTo(p7));
            sink.AddLine(p6);
            if (drawArc) sink.AddArc(RoundedCornerTo(p5));
            sink.AddLine(p4);
            if (drawArc) sink.AddArc(RoundedCornerTo(p3));
            sink.AddLine(p2);
            if (drawArc) sink.AddArc(RoundedCornerTo(p1));
            sink.AddLine(p0);
        }
        else
        {
            sink.BeginFigure(p7, figureBegin);
            if (drawArc) sink.AddArc(RoundedCornerTo(p0));
            sink.AddLine(p1);
            if (drawArc) sink.AddArc(RoundedCornerTo(p2));
            sink.AddLine(p3);
            if (drawArc) sink.AddArc(RoundedCornerTo(p4));
            sink.AddLine(p5);
            if (drawArc) sink.AddArc(RoundedCornerTo(p6));
            sink.AddLine(p7);
        }

        sink.EndFigure(FigureEnd.Closed);
        return;

        ArcSegment RoundedCornerTo(RawVector2 pos)
        {
            return new ArcSegment
            {
                Point = pos,
                Size = new Size2F(rect.RadiusX, rect.RadiusY),
                RotationAngle = 0,
                SweepDirection = counterClockWise ? SweepDirection.CounterClockwise : SweepDirection.Clockwise,
                ArcSize = ArcSize.Small
            };
        }
    }
}