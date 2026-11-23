namespace Rrc.Ledger;

public sealed record LedgerCycle(LedgerReadResult Source, IReadOnlyList<LedgerRecord> Details)
{
    public LedgerRecord Cycle => Source.Record!;
}

public sealed record LedgerLease(LedgerRecord Root, long RootRecordNumber, IReadOnlyList<LedgerCycle> Cycles);

public sealed record LedgerGroupResult(LedgerLease? Lease, LedgerReadResult? Rejected)
{
    public static LedgerGroupResult Accepted(LedgerLease lease) => new(lease, null);

    public static LedgerGroupResult Reject(LedgerReadResult rejected) => new(null, rejected);
}

public static class LedgerLeaseGrouper
{
    private const string ParentRejected = "Parent record was rejected.";

    public static IEnumerable<LedgerGroupResult> Group(IEnumerable<LedgerReadResult> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        PendingLease? lease = null;
        var rootRejected = false;

        foreach (var result in records)
        {
            if (result.Record is not { } record)
            {
                var rejectedType = RecordType(result.RawBytes);

                if (rejectedType == LedgerRecordType.Root)
                {
                    if (lease is not null)
                    {
                        yield return LedgerGroupResult.Accepted(lease.Build());
                        lease = null;
                    }

                    rootRejected = true;
                }
                else if (rejectedType is LedgerRecordType.OilCycle or LedgerRecordType.GasCycle)
                {
                    lease?.AbandonCycle();
                }

                yield return LedgerGroupResult.Reject(result);
                continue;
            }

            if (record.Type == LedgerRecordType.Root)
            {
                if (lease is not null)
                {
                    yield return LedgerGroupResult.Accepted(lease.Build());
                }

                lease = record is LedgerTrailer ? null : new PendingLease(record, result.RecordNumber);
                rootRejected = false;
                continue;
            }

            var error = lease is null
                ? rootRejected ? ParentRejected : "Record appears before any root record."
                : lease.Add(result);

            if (error is not null)
            {
                yield return LedgerGroupResult.Reject(
                    LedgerReadResult.Invalid(result.RecordNumber, result.RawBytes, $"{record.Type} record: {error}"));
            }
        }

        if (lease is not null)
        {
            yield return LedgerGroupResult.Accepted(lease.Build());
        }
    }

    private static LedgerRecordType? RecordType(byte[] bytes) => bytes switch
    {
        [0xF0, 0xF1, ..] => LedgerRecordType.Root,
        [0xF0, 0xF2, ..] => LedgerRecordType.GasCycle,
        [0xF0, 0xF6, ..] => LedgerRecordType.OilCycle,
        _ => null,
    };

    private sealed class PendingLease(LedgerRecord root, long rootRecordNumber)
    {
        private readonly List<LedgerCycle> _cycles = [];
        private List<LedgerRecord>? _details;
        private LedgerRecordType? _cycleType;
        private bool _cycleRejected;

        private LedgerRecordType ExpectedCycleType =>
            root is OilLeaseRoot ? LedgerRecordType.OilCycle : LedgerRecordType.GasCycle;

        public string? Add(LedgerReadResult result)
        {
            var record = result.Record!;

            if (record.Type is LedgerRecordType.OilCycle or LedgerRecordType.GasCycle)
            {
                if (record.Type != ExpectedCycleType)
                {
                    AbandonCycle();
                    return $"Appears under a {root.GetType().Name} root.";
                }

                _details = [];
                _cycleType = record.Type;
                _cycleRejected = false;
                _cycles.Add(new LedgerCycle(result, _details));
                return null;
            }

            var parentType = ParentCycleType(record.Type);

            if (parentType != ExpectedCycleType)
            {
                return $"Appears under a {root.GetType().Name} root.";
            }

            if (_details is null || parentType != _cycleType)
            {
                return _cycleRejected ? ParentRejected : $"No {parentType} record precedes it.";
            }

            _details.Add(record);
            return null;
        }

        public void AbandonCycle()
        {
            _details = null;
            _cycleType = null;
            _cycleRejected = true;
        }

        public LedgerLease Build() => new(root, rootRecordNumber, _cycles);

        private static LedgerRecordType ParentCycleType(LedgerRecordType type) => type switch
        {
            LedgerRecordType.GasDisposition
                or LedgerRecordType.CondensateDisposition
                or LedgerRecordType.GasBalancingPeriod => LedgerRecordType.GasCycle,
            _ => LedgerRecordType.OilCycle,
        };
    }
}
