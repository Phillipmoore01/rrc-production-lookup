namespace Rrc.Ledger.Tests;

public class GoldenFileTests
{
    [Fact]
    public void Decodes_a_lease_extracted_from_the_statewide_oil_historical_ledger()
    {
        using var stream = OpenFixture("oil-lease-sample.ebc");
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

    [Fact]
    public void Decodes_a_gas_well_extracted_from_the_statewide_gas_historical_ledger()
    {
        using var stream = OpenFixture("gas-well-sample.ebc");
        var results = LedgerFileReader.Read(stream).ToList();

        Assert.Equal(14, results.Count);
        Assert.All(results, r => Assert.True(r.IsValid, r.Error));

        var well = Assert.Single(LedgerLeaseGrouper.Group(results)).Lease!;

        Assert.Equal(new GasWellRoot("02", 127175, -8673, 255, 0), well.Root);

        Assert.Equal(
            [
                new GasCycle(new DateOnly(2003, 2, 1), 81565, 70778, 250, "49", "2", true, false, 941, "AB", true, 941, 0, 0, 0, 25, 0),
                new GasCycle(new DateOnly(2003, 1, 1), 81565, 70778, 250, "49", "2", true, false, 1123, "PR", true, 2382, 0, 0, 0, 339, 160),
                new GasCycle(new DateOnly(2002, 12, 1), 81565, 70778, 250, "49", "2", true, false, 0, "PR", true, 2025, 0, 0, 0, 303, 348),
            ],
            well.Cycles.Select(c => c.Cycle));

        Assert.Equal(
            [
                [
                    new Disposition(LedgerRecordType.GasDisposition, 3, 913),
                    new Disposition(LedgerRecordType.GasDisposition, 9, 28),
                    new Disposition(LedgerRecordType.CondensateDisposition, 1, 185),
                    new GasBalancingPeriod(0, 0, 0, 0),
                ],
                [
                    new Disposition(LedgerRecordType.GasDisposition, 3, 2009),
                    new Disposition(LedgerRecordType.GasDisposition, 9, 373),
                    new Disposition(LedgerRecordType.CondensateDisposition, 1, 527),
                ],
                [
                    new Disposition(LedgerRecordType.GasDisposition, 3, 1692),
                    new Disposition(LedgerRecordType.GasDisposition, 9, 333),
                    new Disposition(LedgerRecordType.CondensateDisposition, 1, 187),
                ],
            ],
            well.Cycles.Select(c => c.Details.ToArray()));
    }

    private static FileStream OpenFixture(string name) =>
        File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
}
