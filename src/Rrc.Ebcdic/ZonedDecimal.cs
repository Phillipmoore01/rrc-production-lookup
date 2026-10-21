namespace Rrc.Ebcdic;

public static class ZonedDecimal
{
    private const int UnsignedZone = 0xF;

    public static decimal Decode(ReadOnlySpan<byte> field, int scale = 0)
    {
        if (field.IsEmpty)
        {
            throw new FormatException("Zoned decimal field is empty.");
        }

        decimal value = 0;
        for (var i = 0; i < field.Length; i++)
        {
            var zone = field[i] >> 4;
            var digit = field[i] & 0x0F;

            if (digit > 9)
            {
                throw new FormatException($"Invalid zoned decimal digit 0x{digit:X} at byte {i}.");
            }

            value = value * 10 + digit;

            if (i < field.Length - 1 && zone != UnsignedZone)
            {
                throw new FormatException($"Invalid zone 0x{zone:X} at byte {i}.");
            }

            if (i == field.Length - 1)
            {
                value = SignNibble.Apply(zone, value, i);
            }
        }

        return Scale.Apply(value, scale);
    }
}
