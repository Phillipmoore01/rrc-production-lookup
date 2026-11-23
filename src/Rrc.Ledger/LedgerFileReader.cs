namespace Rrc.Ledger;

public sealed record LedgerReadResult(long RecordNumber, byte[] RawBytes, LedgerRecord? Record, string? Error)
{
    public bool IsValid => Record is not null;

    public static LedgerReadResult Valid(long recordNumber, byte[] rawBytes, LedgerRecord record) =>
        new(recordNumber, rawBytes, record, null);

    public static LedgerReadResult Invalid(long recordNumber, byte[] rawBytes, string error) =>
        new(recordNumber, rawBytes, null, error);
}

public static class LedgerFileReader
{
    public static IEnumerable<LedgerReadResult> Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        long recordNumber = 0;

        while (true)
        {
            var buffer = new byte[LedgerRecordParser.RecordLength];
            var bytesRead = stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
            if (bytesRead == 0)
            {
                yield break;
            }

            recordNumber++;

            if (bytesRead < buffer.Length)
            {
                yield return LedgerReadResult.Invalid(
                    recordNumber,
                    buffer[..bytesRead],
                    $"Trailing partial record of {bytesRead} bytes; expected {LedgerRecordParser.RecordLength}.");
                yield break;
            }

            yield return ParseRecord(recordNumber, buffer);
        }
    }

    private static LedgerReadResult ParseRecord(long recordNumber, byte[] buffer)
    {
        try
        {
            return LedgerReadResult.Valid(recordNumber, buffer, LedgerRecordParser.Parse(buffer));
        }
        catch (LedgerFormatException ex)
        {
            return LedgerReadResult.Invalid(recordNumber, buffer, ex.Message);
        }
    }
}
