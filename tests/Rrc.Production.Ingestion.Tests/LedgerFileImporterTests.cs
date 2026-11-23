using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Rrc.Ledger;
using Rrc.Ledger.Tests;
using Rrc.Production.Data;

namespace Rrc.Production.Ingestion.Tests;

public sealed class LedgerFileImporterTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly RecordingQuarantine _quarantine = new();

    public LedgerFileImporterTests()
    {
        _connection.Open();
        using var db = CreateContext();
        db.Database.EnsureCreated();
    }

    [Fact]
    public async Task Loads_oil_and_gas_leases_with_monthly_production()
    {
        var file = Concat(
            OilRoot(8, 12345),
            OilCycle(9927, operatorNumber: 111111, oil: 2875, casinghead: 4410),
            OilDisposition(2875),
            OilCycle(9926, operatorNumber: 111111, oil: 2700, casinghead: 4300),
            GasRoot(11, 204417),
            GasCycle(9927, operatorNumber: 222222, gas: 28765, condensate: 312));

        var summary = await ImportAsync(file);

        Assert.Equal(new LedgerImportSummary(Leases: 2, Months: 3, Rejected: 0), summary);

        using var db = CreateContext();
        var oil = await db.Leases.Include(l => l.ProductionMonths).SingleAsync(l => l.WellType == WellType.Oil);
        Assert.Equal(("7B", "12345"), (oil.DistrictCode, oil.LeaseNumber));
        var january = oil.ProductionMonths.Single(m => m.Month == new DateOnly(1993, 1, 1));
        Assert.Equal((111111, 2875m, 4410m), (january.OperatorNumber, january.OilBarrels, january.CasingheadGasMcf));

        var gas = await db.Leases.Include(l => l.ProductionMonths).SingleAsync(l => l.WellType == WellType.Gas);
        Assert.Equal(("8A", "204417"), (gas.DistrictCode, gas.LeaseNumber));
        var gasMonth = Assert.Single(gas.ProductionMonths);
        Assert.Equal((28765m, 312m), (gasMonth.GasMcf, gasMonth.CondensateBarrels));
    }

    [Fact]
    public async Task Quarantines_malformed_orphaned_and_duplicate_records()
    {
        var file = Concat(
            OilDisposition(10),
            OilRoot(8, 12345),
            OilCycle(9927, operatorNumber: 1, oil: 100, casinghead: 0),
            OilCycle(9927, operatorNumber: 1, oil: 999, casinghead: 0),
            new LedgerRecordBuilder("99").Build());

        var summary = await ImportAsync(file);

        Assert.Equal(new LedgerImportSummary(Leases: 1, Months: 1, Rejected: 3), summary);
        Assert.Equal([1L, 4L, 5L], _quarantine.Records.Select(r => r.RecordNumber).Order());
        Assert.All(_quarantine.Records, r => Assert.Equal(LedgerRecordParser.RecordLength, r.RawBytes.Length));

        using var db = CreateContext();
        Assert.Equal(100m, (await db.ProductionMonths.SingleAsync()).OilBarrels);
    }

    [Fact]
    public async Task Reloading_a_lease_updates_existing_months_and_adds_new_ones()
    {
        await ImportAsync(Concat(OilRoot(8, 12345), OilCycle(9927, operatorNumber: 1, oil: 100, casinghead: 0)));

        await ImportAsync(Concat(
            OilRoot(8, 12345),
            OilCycle(9927, operatorNumber: 2, oil: 150, casinghead: 0),
            OilCycle(9926, operatorNumber: 2, oil: 90, casinghead: 0)));

        using var db = CreateContext();
        var lease = await db.Leases.Include(l => l.ProductionMonths).SingleAsync();
        Assert.Equal(
            [(new DateOnly(1993, 1, 1), 2, 150m), (new DateOnly(1993, 2, 1), 2, 90m)],
            lease.ProductionMonths.OrderBy(m => m.Month).Select(m => (m.Month, m.OperatorNumber, m.OilBarrels)));
    }

    [Fact]
    public async Task Saves_across_multiple_batches()
    {
        var file = Concat(Enumerable.Range(1, 5)
            .SelectMany(n => new[] { OilRoot(1, n), OilCycle(9927, operatorNumber: 1, oil: n, casinghead: 0) })
            .ToArray());

        var summary = await ImportAsync(file, batchSize: 2);

        Assert.Equal(5, summary.Leases);
        using var db = CreateContext();
        Assert.Equal(5, await db.Leases.CountAsync());
    }

    public void Dispose() => _connection.Dispose();

    private async Task<LedgerImportSummary> ImportAsync(byte[] file, int batchSize = 500)
    {
        using var db = CreateContext();
        var importer = new LedgerFileImporter(db, Options.Create(new LedgerImportOptions { BatchSize = batchSize }));
        return await importer.ImportAsync(new MemoryStream(file), _quarantine, CancellationToken.None);
    }

    private ProductionDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ProductionDbContext>().UseSqlite(_connection).Options);

    private static byte[] Concat(params byte[][] records) => records.SelectMany(r => r).ToArray();

    private static byte[] OilRoot(int districtTapeValue, int leaseNumber) => new LedgerRecordBuilder("01")
        .Text(3, "O").Zoned(4, 2, districtTapeValue).Zoned(6, 6, leaseNumber)
        .Packed(12, 0).Packed(17, 0).Packed(22, 0).Packed(27, 0)
        .Build();

    private static byte[] GasRoot(int districtTapeValue, int rrcId) => new LedgerRecordBuilder("01")
        .Text(3, "G").Zoned(4, 2, districtTapeValue).Zoned(6, 6, rrcId)
        .Packed(12, 0).Packed(17, 0).Packed(22, 0)
        .Build();

    private static byte[] OilCycle(int cycleKey, int operatorNumber, long oil, long casinghead)
    {
        var builder = new LedgerRecordBuilder("06")
            .Zoned(3, 4, cycleKey).Zoned(7, 6, operatorNumber).Zoned(13, 5, 1).Zoned(18, 3, 1)
            .Zoned(24, 3, 0).Zoned(27, 3, 0)
            .Packed(36, oil).Packed(61, casinghead);

        foreach (var position in new[] { 31, 41, 46, 51, 56, 66, 71 })
        {
            builder.Packed(position, 0);
        }

        return builder.Build();
    }

    private static byte[] GasCycle(int cycleKey, int operatorNumber, long gas, long condensate)
    {
        var builder = new LedgerRecordBuilder("02")
            .Zoned(3, 4, cycleKey).Zoned(7, 6, operatorNumber).Zoned(13, 5, 1).Zoned(18, 3, 1)
            .Packed(70, gas).Packed(124, condensate);

        foreach (var position in new[] { 50, 81, 114, 119, 129 })
        {
            builder.Packed(position, 0);
        }

        return builder.Build();
    }

    private static byte[] OilDisposition(long amount) =>
        new LedgerRecordBuilder("07").Zoned(3, 2, 1).Packed(5, amount).Build();

    private sealed class RecordingQuarantine : ILedgerQuarantine
    {
        public List<LedgerReadResult> Records { get; } = [];

        public void Add(LedgerReadResult rejected) => Records.Add(rejected);
    }
}
