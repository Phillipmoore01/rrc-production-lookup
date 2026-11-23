namespace Rrc.Ledger.Tests;

public class GoldenFileTests
{
    private static readonly string FixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "oil-lease-sample.ebc");

    [Fact]
    public void Decodes_a_lease_extracted_from_the_statewide_oil_historical_ledger()
    {
        using var stream = File.OpenRead(FixturePath);
        var results = LedgerFileReader.Read(stream).ToList();

        Assert.Equal(10, results.Count);
        Assert.All(results, r => Assert.True(r.IsValid, r.Error));

        var lease = Assert.Single(LedgerLeaseGrouper.Group(results)).Lease!;

        Assert.Equal(new OilLeaseRoot("01", 737, 52, 0, 0, 113), lease.Root);

        Assert.Equal(
            [
                new OilCycle(new DateOnly(2024, 10, 1), 100797, 47358, 1, 'R', true, false, 0, 1, true, 434, 151, 75, 0, 75, 28148, 112, 0, 0),
                new OilCycle(new DateOnly(2024, 9, 1), 100797, 47358, 1, 'R', true, false, 0, 1, true, 420, 221, 36, 0, 36, 27240, 63, 0, 0),
                new OilCycle(new DateOnly(2024, 8, 1), 100797, 47358, 1, 'R', true, false, 0, 1, true, 403, 190, 88, 0, 88, 28148, 51, 0, 0),
            ],
            lease.Cycles.Select(c => c.Cycle));

        Assert.Equal(
            [
                [new Disposition(LedgerRecordType.OilDisposition, 1, 112), new Disposition(LedgerRecordType.CasingheadDisposition, 3, 112)],
                [new Disposition(LedgerRecordType.OilDisposition, 1, 273), new Disposition(LedgerRecordType.CasingheadDisposition, 3, 63)],
                [new Disposition(LedgerRecordType.OilDisposition, 1, 152), new Disposition(LedgerRecordType.CasingheadDisposition, 3, 51)],
            ],
            lease.Cycles.Select(c => c.Details.ToArray()));
    }
}
