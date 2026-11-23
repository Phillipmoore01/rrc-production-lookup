namespace Rrc.Ledger.Tests;

public class LedgerFileReaderTests
{
    [Fact]
    public void Reads_consecutive_records_and_quarantines_malformed_ones()
    {
        var root = new LedgerRecordBuilder("01")
            .Text(3, "O").Zoned(4, 2, 8).Zoned(6, 6, 12345)
            .Packed(12, 0).Packed(17, 0).Packed(22, 0).Packed(27, 0)
            .Build();
        var malformed = new LedgerRecordBuilder("99").Build();
        var disposition = new LedgerRecordBuilder("07").Zoned(3, 2, 1).Packed(5, 500).Build();
        var partial = new byte[10];

        using var stream = new MemoryStream([.. root, .. malformed, .. disposition, .. partial]);
        var results = LedgerFileReader.Read(stream).ToList();

        Assert.Collection(
            results,
            r => Assert.IsType<OilLeaseRoot>(r.Record),
            r =>
            {
                Assert.False(r.IsValid);
                Assert.Equal(2, r.RecordNumber);
                Assert.Equal(malformed, r.RejectedBytes);
                Assert.Contains("'99'", r.Error);
            },
            r => Assert.Equal(new Disposition(LedgerRecordType.OilDisposition, 1, 500), r.Record),
            r =>
            {
                Assert.False(r.IsValid);
                Assert.Equal(4, r.RecordNumber);
                Assert.Equal(10, r.RejectedBytes!.Length);
            });
    }

    [Fact]
    public void Yields_nothing_for_an_empty_stream()
    {
        Assert.Empty(LedgerFileReader.Read(new MemoryStream()));
    }
}
