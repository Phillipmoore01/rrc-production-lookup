namespace Rrc.Ebcdic;

public static class PackedDecimal
{
    public static decimal Decode(ReadOnlySpan<byte> field, int scale = 0)
    {
        if (field.IsEmpty)
        {
            throw new FormatException("Packed decimal field is empty.");
        }

        decimal value = 0;
        for (var i = 0; i < field.Length; i++)
        {
            var high = field[i] >> 4;
            var low = field[i] & 0x0F;

            value = value * 10 + Digit(high, i);

            if (i < field.Length - 1)
            {
                value = value * 10 + Digit(low, i);
            }
            else
            {
                value = SignNibble.Apply(low, value, i);
            }
        }

        return Scale.Apply(value, scale);
    }

    private static int Digit(int nibble, int offset) =>
        nibble <= 9 ? nibble : throw new FormatException($"Invalid packed decimal digit 0x{nibble:X} at byte {offset}.");
}
