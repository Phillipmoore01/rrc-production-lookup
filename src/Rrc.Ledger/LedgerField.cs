using Rrc.Ebcdic;

namespace Rrc.Ledger;

internal static class LedgerField
{
    private const int PackedNineDigitLength = 5;

    public static string Text(ReadOnlySpan<byte> record, int position, int length) =>
        EbcdicText.Decode(Slice(record, position, length));

    public static char Character(ReadOnlySpan<byte> record, int position)
    {
        var text = Text(record, position, 1);
        return text.Length == 0 ? ' ' : text[0];
    }

    public static int Number(ReadOnlySpan<byte> record, int position, int length)
    {
        try
        {
            return (int)ZonedDecimal.Decode(Slice(record, position, length));
        }
        catch (FormatException ex)
        {
            throw InvalidField(position, ex);
        }
    }

    public static long Packed(ReadOnlySpan<byte> record, int position)
    {
        try
        {
            return (long)PackedDecimal.Decode(Slice(record, position, PackedNineDigitLength));
        }
        catch (FormatException ex)
        {
            throw InvalidField(position, ex);
        }
    }

    private static LedgerFormatException InvalidField(int position, FormatException inner) =>
        new($"Invalid numeric field at position {position}: {inner.Message}", inner);

    private static ReadOnlySpan<byte> Slice(ReadOnlySpan<byte> record, int position, int length) =>
        record.Slice(position - 1, length);
}
