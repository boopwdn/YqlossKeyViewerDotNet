// using System.Drawing;
// using System.Drawing.Drawing2D;
// using System.Drawing.Imaging;
// using System.Drawing.Text;
// using Vanara.PInvoke;
// using YqlossKeyViewerDotNet.Elements;
// using YqlossKeyViewerDotNet.Utils;
// using YqlossKeyViewerDotNet.Windows;
//
// namespace YqlossKeyViewerDotNet.RenderEngines;
//
// public class GdiPlusRenderEngine : IRenderEngine
// {
//     private static Gdi32.BLENDFUNCTION BlendFunction { get; } = new()
//     {
//         AlphaFormat = 1, // AC_SRC_ALPHA
//         BlendFlags = 0,
//         BlendOp = 0, // AC_SRC_OVER
//         SourceConstantAlpha = 255
//     };
//
//     private static POINT Origin { get; } = new(0, 0);
//     private SIZE ClientSize { get; set; }
//
//     private User32.SafeHWND HWnd { get; }
//     private User32.SafeReleaseHDC HDc { get; set; } = SuppressUtil.LateInit<User32.SafeReleaseHDC>();
//     private bool CDcInitialized { get; set; }
//     private Gdi32.SafeHDC CDc { get; set; } = SuppressUtil.LateInit<Gdi32.SafeHDC>();
//     private Gdi32.SafeHBITMAP HBitmap { get; set; } = SuppressUtil.LateInit<Gdi32.SafeHBITMAP>();
//     private Graphics Graphics { get; set; } = SuppressUtil.LateInit<Graphics>();
//
//     private DefaultDictionary<string, string, Image> ImageMap { get; }
//
//     public GdiPlusRenderEngine(Window window)
//     {
//         HWnd = window.HWnd;
//
//         ImageMap = new DefaultDictionary<string, string, Image>
//         {
//             KeyTransformer = it => it,
//             DefaultValue = (imagePath, _) => Image.FromFile(imagePath)
//         };
//     }
//
//     public void BeginDraw()
//     {
//         HDc = User32.GetDC(HWnd);
//
//         if (!CDcInitialized)
//         {
//             User32.GetClientRect(HWnd, out var clientRect);
//             ClientSize = new SIZE(clientRect.Width, clientRect.Height);
//             CDc = Gdi32.CreateCompatibleDC(HDc);
//             HBitmap = Gdi32.CreateCompatibleBitmap(HDc, clientRect.Width, clientRect.Height);
//             Gdi32.DeleteObject(Gdi32.SelectObject(CDc, HBitmap));
//             Graphics = Graphics.FromHdc(CDc.DangerousGetHandle());
//             Graphics.SmoothingMode = SmoothingMode.AntiAlias;
//             Graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
//             CDcInitialized = true;
//         }
//
//         Graphics.Clear(Color.Transparent);
//     }
//
//     public void EndDraw()
//     {
//         User32.UpdateLayeredWindow(
//             HWnd,
//             HDc,
//             new POINT(KeyViewer.Instance.Storage.WindowX, KeyViewer.Instance.Storage.WindowY),
//             ClientSize,
//             CDc,
//             Origin,
//             0,
//             BlendFunction,
//             User32.UpdateLayeredWindowFlags.ULW_ALPHA
//         );
//         HDc.Dispose();
//     }
//
//     public void SetClip(
//         double x,
//         double y,
//         double width,
//         double height
//     )
//     {
//         Graphics.SetClip(new RectangleF((float)x, (float)y, (float)width, (float)height));
//     }
//
//     public void ResetClip()
//     {
//         Graphics.ResetClip();
//     }
//
//     private void DrawBorderedRectBrush(
//         double x,
//         double y,
//         double width,
//         double height,
//         double borderWidth,
//         double borderHeight,
//         double borderRadius,
//         double innerRadius,
//         Brush brushBorder,
//         Brush brushInner
//     )
//     {
//         var actualBorderWidth = Math.Min(borderWidth, width / 2);
//         var actualBorderHeight = Math.Min(borderHeight, height / 2);
//         using var pathInner = new GraphicsPath();
//         DrawRoundedRectOnPath(pathInner, x, y, width, height, borderRadius);
//         Graphics.FillPath(brushInner, pathInner);
//         if (actualBorderWidth <= 0.0 && actualBorderHeight <= 0.0) return;
//         using var pathBorder = new GraphicsPath();
//         pathBorder.FillMode = FillMode.Alternate;
//         DrawRoundedRectOnPath(pathBorder, x, y, width, height, borderRadius);
//         DrawRoundedRectOnPath(pathBorder,
//             x + actualBorderWidth,
//             y + actualBorderHeight,
//             width - actualBorderWidth * 2,
//             height - actualBorderHeight * 2,
//             innerRadius
//         );
//         Graphics.FillPath(brushBorder, pathBorder);
//     }
//
//     public void DrawBorderedRect(
//         double x,
//         double y,
//         double width,
//         double height,
//         double borderWidth,
//         double borderHeight,
//         double borderRadius,
//         double innerRadius,
//         JsonColor borderColor,
//         JsonColor innerColor
//     )
//     {
//         using var brushInner = new SolidBrush(ToGdiColor(innerColor));
//         using var brushBorder = new SolidBrush(ToGdiColor(borderColor));
//         DrawBorderedRectBrush(
//             x,
//             y,
//             width,
//             height,
//             borderWidth,
//             borderHeight,
//             borderRadius,
//             innerRadius,
//             brushBorder,
//             brushInner
//         );
//     }
//
//     public void DrawFadeBorderedRect(
//         double x,
//         double y,
//         double width,
//         double height,
//         double borderWidth,
//         double borderHeight,
//         double borderRadius,
//         double innerRadius,
//         JsonColor borderColor,
//         JsonColor innerColor,
//         double fadeBeginX,
//         double fadeBeginY,
//         double fadeEndX,
//         double fadeEndY
//     )
//     {
//         var clipLength = 2 * (ClientSize.Width + ClientSize.Height);
//         using var pathBegin = MakeSplitGraphicsPath(fadeBeginX, fadeBeginY, fadeEndX, fadeEndY, clipLength);
//         using var pathEnd = MakeSplitGraphicsPath(fadeEndX, fadeEndY, fadeBeginX, fadeBeginY, clipLength, true);
//         var pointBegin = new PointF((float)fadeBeginY, (float)fadeBeginY);
//         var pointEnd = new PointF((float)fadeEndX, (float)fadeEndY);
//         using var brushInnerGradient = new LinearGradientBrush(
//             pointBegin,
//             pointEnd,
//             Color.Transparent,
//             ToGdiColor(innerColor)
//         );
//         using var brushBorderGradient = new LinearGradientBrush(
//             pointBegin,
//             pointEnd,
//             Color.Transparent,
//             ToGdiColor(borderColor)
//         );
//         Graphics.SetClip(pathBegin, CombineMode.Replace);
//         Graphics.SetClip(pathEnd, CombineMode.Exclude);
//         DrawBorderedRectBrush(
//             x,
//             y,
//             width,
//             height,
//             borderWidth,
//             borderHeight,
//             borderRadius,
//             innerRadius,
//             brushBorderGradient,
//             brushInnerGradient
//         );
//         Graphics.ResetClip();
//         using var brushInnerSolid = new SolidBrush(ToGdiColor(innerColor));
//         using var brushBorderSolid = new SolidBrush(ToGdiColor(borderColor));
//         Graphics.SetClip(pathEnd, CombineMode.Replace);
//         DrawBorderedRectBrush(
//             x,
//             y,
//             width,
//             height,
//             borderWidth,
//             borderHeight,
//             borderRadius,
//             innerRadius,
//             brushBorderSolid,
//             brushInnerSolid
//         );
//         Graphics.ResetClip();
//     }
//
//     private static GraphicsPath MakeSplitGraphicsPath(
//         double originX,
//         double originY,
//         double normalX,
//         double normalY,
//         double length,
//         bool opposite = false
//     )
//     {
//         if (opposite)
//         {
//             normalX = 2 * originX - normalX;
//             normalY = 2 * originY - normalY;
//         }
//
//         var diffX = normalX - originX;
//         var diffY = normalY - originY;
//         var diffLength = Math.Sqrt(diffX * diffX + diffY * diffY);
//         var oX = (float)originX;
//         var oY = (float)originY;
//         var nX = (float)(diffX / diffLength * length);
//         var nY = (float)(diffY / diffLength * length);
//
//         var path = new GraphicsPath();
//         path.AddLines(
//             new PointF(oX - nY, oY + nX),
//             new PointF(oX + nY, oY - nX),
//             new PointF(oX + nY + nX * 2, oY - nX + nY * 2),
//             new PointF(oX - nY + nX * 2, oY + nX + nY * 2)
//         );
//         path.CloseFigure();
//         return path;
//     }
//
//     private static void DrawRoundedRectOnPath(
//         GraphicsPath path,
//         double x,
//         double y,
//         double width,
//         double height,
//         double radius
//     )
//     {
//         var actualRadius = (float)Math.Min(radius, Math.Min(width / 2, height / 2));
//         var lFloat = (float)x;
//         var uFloat = (float)y;
//         var rFloat = lFloat + (float)width;
//         var dFloat = uFloat + (float)height;
//         var arcSize = actualRadius * 2;
//         if (arcSize > 0) path.AddArc(lFloat, uFloat, arcSize, arcSize, 180, 90);
//         path.AddLine(lFloat + actualRadius, uFloat, rFloat - actualRadius, uFloat);
//         path.AddLine(lFloat + actualRadius, uFloat, rFloat - actualRadius, uFloat);
//         if (arcSize > 0) path.AddArc(rFloat - arcSize, uFloat, arcSize, arcSize, 270, 90);
//         path.AddLine(rFloat, uFloat + actualRadius, rFloat, dFloat - actualRadius);
//         if (arcSize > 0) path.AddArc(rFloat - arcSize, dFloat - arcSize, arcSize, arcSize, 0, 90);
//         path.AddLine(rFloat - actualRadius, dFloat, lFloat + actualRadius, dFloat);
//         if (arcSize > 0) path.AddArc(lFloat, dFloat - arcSize, arcSize, arcSize, 90, 90);
//         path.AddLine(lFloat, dFloat - actualRadius, lFloat, uFloat + actualRadius);
//         path.CloseFigure();
//     }
//
//     public void DrawImage(
//         string imagePath,
//         double x,
//         double y,
//         double width,
//         double height,
//         double radius,
//         double alpha
//     )
//     {
//         using var path = new GraphicsPath();
//         DrawRoundedRectOnPath(path, 0, 0, width, height, radius);
//         using var attributes = new ImageAttributes();
//         attributes.SetWrapMode(WrapMode.TileFlipXY);
//         attributes.SetColorMatrix(new ColorMatrix().Also(m => m.Matrix33 = (float)alpha));
//         var image = ImageMap[imagePath];
//         using var brush = new TextureBrush(
//             image,
//             new RectangleF(0, 0, image.Width, image.Height),
//             attributes
//         );
//         brush.ScaleTransform((float)width / image.Width, (float)height / image.Height);
//         var matrix = Graphics.TransformElements;
//         Graphics.TranslateTransform((float)x, (float)y);
//         Graphics.FillPath(brush, path);
//         Graphics.TransformElements = matrix;
//     }
//
//     public void DrawText(
//         string text,
//         double x,
//         double y,
//         double width,
//         double height,
//         double anchorX,
//         double anchorY,
//         string fontName,
//         bool bold,
//         bool italic,
//         double fontSize,
//         JsonColor color,
//         double maxFontSize
//     )
//     {
//     }
//
//     private static Color ToGdiColor(JsonColor jsonColor)
//     {
//         return Color.FromArgb(jsonColor.A, jsonColor.R, jsonColor.G, jsonColor.B);
//     }
// }

