namespace YqlossKeyViewerDotNet.Utils;

public static class BitUtil
{
    public static ushort LowWord(uint value)
    {
        return (ushort)(value & 0xFFFF);
    }

    public static ushort HighWord(uint value)
    {
        return (ushort)((value >> 16) & 0xFFFF);
    }
}