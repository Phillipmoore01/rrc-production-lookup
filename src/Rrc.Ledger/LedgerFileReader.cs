namespace Rrc.Ledger;

public sealed record LedgerReadResult(long RecordNumber, LedgerRecord? Record, byte[]? RejectedBytes, string? Error)
{
    public bool IsValid => Record is not null;
}

public static class LedgerFileReader
{
    public static IEnumerable<LedgerReadResult> Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var buffer = new byte[LedgerRecordParser.RecordLength];
        long recordNumber = 0;

        while (true)
        {
            var bytesRead = stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
            if (bytesRead == 0)
            {
                yield break;
            }

            recordNumber++;

            if (bytesRead < buffer.Length)
            {
                yield return Rejected(
                    recordNumber,
                    buffer.AsSpan(0, bytesRead),
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
            return new LedgerReadResult(recordNumber, LedgerRecordParser.Parse(buffer), null, null);
        }
        catch (LedgerFormatException ex)
        {
            return Rejected(recordNumber, buffer, ex.Message);
        }
    }

    private static LedgerReadResult Rejected(long recordNumber, ReadOnlySpan<byte> bytes, string error) =>
        new(recordNumber, null, bytes.ToArray(), error);
}
