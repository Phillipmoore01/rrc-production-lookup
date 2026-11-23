using System.Globalization;
using Rrc.Ledger;
using Rrc.Production.Data;

namespace Rrc.Production.Ingestion;

public sealed record MappedLease(Lease Lease, IReadOnlyList<LedgerReadResult> Rejected);

public static class LedgerLeaseMapper
{
    public static MappedLease Map(LedgerLease ledgerLease)
    {
        var lease = ledgerLease.Root switch
        {
            OilLeaseRoot oil => NewLease(oil.District, oil.LeaseNumber, WellType.Oil),
            GasWellRoot gas => NewLease(gas.District, gas.RrcId, WellType.Gas),
            var root => throw new ArgumentException($"Unsupported root record {root.GetType().Name}.", nameof(ledgerLease)),
        };

        var rejected = new List<LedgerReadResult>();
        var months = new HashSet<DateOnly>();

        foreach (var cycle in ledgerLease.Cycles)
        {
            var month = ToMonth(cycle.Cycle);

            if (!months.Add(month.Month))
            {
                rejected.Add(LedgerReadResult.Invalid(
                    cycle.Source.RecordNumber,
                    cycle.Source.RawBytes,
                    $"Duplicate cycle for {month.Month:yyyy-MM} on lease {lease.DistrictCode}-{lease.LeaseNumber}."));
                continue;
            }

            lease.ProductionMonths.Add(month);
        }

        return new MappedLease(lease, rejected);
    }

    private static Lease NewLease(string district, int number, WellType wellType) => new()
    {
        DistrictCode = district,
        LeaseNumber = number.ToString(CultureInfo.InvariantCulture),
        WellType = wellType,
    };

    private static LeaseProductionMonth ToMonth(LedgerRecord cycle) => cycle switch
    {
        OilCycle oil => new LeaseProductionMonth
        {
            Month = oil.Month,
            OperatorNumber = oil.OperatorNumber,
            OilBarrels = oil.OilProductionBarrels,
            CasingheadGasMcf = oil.CasingheadProductionMcf,
        },
        GasCycle gas => new LeaseProductionMonth
        {
            Month = gas.Month,
            OperatorNumber = gas.OperatorNumber,
            GasMcf = gas.GasProductionMcf,
            CondensateBarrels = gas.CondensateLeaseProductionBarrels,
        },
        _ => throw new ArgumentException($"Unsupported cycle record {cycle.GetType().Name}.", nameof(cycle)),
    };
}
