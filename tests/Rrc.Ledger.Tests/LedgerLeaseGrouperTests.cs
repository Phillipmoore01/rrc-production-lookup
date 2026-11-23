namespace Rrc.Ledger.Tests;

public class LedgerLeaseGrouperTests
{
    private static readonly OilLeaseRoot OilRoot = new("08", 1, 0, 0, 0, 0);
    private static readonly GasWellRoot GasRoot = new("01", 2, 0, 0, 0);
    private static readonly OilCycle OilMonth = new(
        new DateOnly(1993, 1, 1), 1, 1, 1, 'R', true, false, 1, 0, false, 0, 100, 0, 0, 0, 0, 50, 0, 0);
    private static readonly GasCycle GasMonth = new(
        new DateOnly(1993, 1, 1), 1, 1, 1, "PR", "1", true, false, 0, "G", false, 900, 0, 0, 0, 10, 0);
    private static readonly Disposition OilSale = new(LedgerRecordType.OilDisposition, 1, 100);
    private static readonly Disposition GasSale = new(LedgerRecordType.GasDisposition, 2, 900);

    [Fact]
    public void Groups_cycles_and_their_details_under_each_root()
    {
        var results = Group(OilRoot, OilMonth, OilSale, OilMonth, GasRoot, GasMonth, GasSale);

        Assert.Collection(
            results,
            r =>
            {
                Assert.Equal(OilRoot, r.Lease!.Root);
                Assert.Equal(1, r.Lease.RootRecordNumber);
                Assert.Equal(2, r.Lease.Cycles.Count);
                Assert.Equal([OilSale], r.Lease.Cycles[0].Details);
                Assert.Empty(r.Lease.Cycles[1].Details);
            },
            r =>
            {
                Assert.Equal(GasRoot, r.Lease!.Root);
                Assert.Equal(5, r.Lease.RootRecordNumber);
                Assert.Equal(GasMonth, Assert.Single(r.Lease.Cycles).Cycle);
                Assert.Equal([GasSale], r.Lease.Cycles[0].Details);
            });
    }

    [Fact]
    public void Rejects_records_that_appear_before_any_root()
    {
        var results = Group(OilMonth, OilRoot);

        Assert.Equal(1, results[0].Rejected!.RecordNumber);
        Assert.Contains("before any root", results[0].Rejected!.Error);
        Assert.NotNull(results[1].Lease);
    }

    [Fact]
    public void Rejects_cycles_that_do_not_match_the_root_kind()
    {
        var results = Group(OilRoot, GasMonth, GasSale);

        Assert.Empty(results.Single(r => r.Lease is not null).Lease!.Cycles);
        Assert.Equal([2L, 3L], results.Where(r => r.Rejected is not null).Select(r => r.Rejected!.RecordNumber));
    }

    [Fact]
    public void Rejects_details_with_no_preceding_cycle()
    {
        var results = Group(OilRoot, OilSale);

        Assert.Contains("No OilCycle record precedes it", results.Single(r => r.Rejected is not null).Rejected!.Error);
    }

    [Fact]
    public void Rejects_the_children_of_a_rejected_root()
    {
        var input = new[]
        {
            Valid(1, OilRoot),
            Valid(2, OilMonth),
            Invalid(3, 0xF0, 0xF1),
            Valid(4, OilMonth),
            Valid(5, OilSale),
            Valid(6, OilRoot),
        };

        var results = LedgerLeaseGrouper.Group(input).ToList();

        Assert.Equal(2, results.Count(r => r.Lease is not null));
        var rejected = results.Where(r => r.Rejected is not null).Select(r => r.Rejected!).ToList();
        Assert.Equal([3L, 4L, 5L], rejected.Select(r => r.RecordNumber));
        Assert.All(rejected.Skip(1), r => Assert.Contains("Parent record was rejected", r.Error));
    }

    [Fact]
    public void Does_not_attach_details_of_a_rejected_cycle_to_the_previous_cycle()
    {
        var input = new[]
        {
            Valid(1, OilRoot),
            Valid(2, OilMonth),
            Invalid(3, 0xF0, 0xF6),
            Valid(4, OilSale),
        };

        var results = LedgerLeaseGrouper.Group(input).ToList();

        var lease = results.Single(r => r.Lease is not null).Lease!;
        Assert.Empty(Assert.Single(lease.Cycles).Details);
        var orphan = results.Where(r => r.Rejected is not null).Last().Rejected!;
        Assert.Equal(4, orphan.RecordNumber);
        Assert.Contains("Parent record was rejected", orphan.Error);
    }

    private static List<LedgerGroupResult> Group(params LedgerRecord[] records) =>
        LedgerLeaseGrouper.Group(records.Select((r, i) => Valid(i + 1, r))).ToList();

    private static LedgerReadResult Valid(long number, LedgerRecord record) =>
        LedgerReadResult.Valid(number, new byte[LedgerRecordParser.RecordLength], record);

    private static LedgerReadResult Invalid(long number, params byte[] prefix)
    {
        var bytes = new byte[LedgerRecordParser.RecordLength];
        prefix.CopyTo(bytes, 0);
        return LedgerReadResult.Invalid(number, bytes, "Malformed.");
    }
}
