namespace Rrc.Ebcdic;

internal static class SignNibble
{
    public static decimal Apply(int nibble, decimal magnitude, int offset) => nibble switch
    {
        0xC or 0xF or 0xA or 0xE => magnitude,
        0xD or 0xB => -magnitude,
        _ => throw new FormatException($"Invalid sign nibble 0x{nibble:X} at byte {offset}."),
    };
}
