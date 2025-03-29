namespace YqlossKeyViewerDotNet.Elements;

public struct JsonColor
{
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
    public byte A { get; set; } = 255;

    public JsonColor()
    {
    }
}