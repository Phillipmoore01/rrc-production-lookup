using System.Globalization;
using System.Text;

namespace Rrc.Ledger.Tests;

internal sealed class LedgerRecordBuilder
{
    private const byte EbcdicSpace = 0x40;

    private static readonly Encoding Ebcdic = CreateEncoding();

    private readonly byte[] _record = Enumerable.Repeat(EbcdicSpace, LedgerRecordParser.RecordLength).ToArray();

    public LedgerRecordBuilder(string recordId) => Text(1, recordId);

    public LedgerRecordBuilder Text(int position, string value)
    {
        Ebcdic.GetBytes(value).CopyTo(_record, position - 1);
        return this;
    }

    public LedgerRecordBuilder Zoned(int position, int length, long value) =>
        Text(position, value.ToString(CultureInfo.InvariantCulture).PadLeft(length, '0'));

    public LedgerRecordBuilder Packed(int position, long value)
    {
        var digits = Math.Abs(value).ToString(CultureInfo.InvariantCulture).PadLeft(9, '0');
        var nibbles = digits.Select(d => d - '0').Append(value < 0 ? 0xD : 0xC).ToArray();

        for (var i = 0; i < 5; i++)
        {
            _record[position - 1 + i] = (byte)((nibbles[2 * i] << 4) | nibbles[2 * i + 1]);
        }

        return this;
    }

    public LedgerRecordBuilder Raw(int position, params byte[] bytes)
    {
        bytes.CopyTo(_record, position - 1);
        return this;
    }

    public byte[] Build() => (byte[])_record.Clone();

    private static Encoding CreateEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(37);
    }
}
