using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Rrc.Ledger;
using Rrc.Production.Data;

namespace Rrc.Production.Ingestion;

public sealed record LedgerImportSummary(int Leases, int Months, int Rejected);

public interface ILedgerFileImporter
{
    Task<LedgerImportSummary> ImportAsync(Stream ledger, ILedgerQuarantine quarantine, CancellationToken cancellationToken);
}

public class LedgerFileImporter(ProductionDbContext db, IOptions<LedgerImportOptions> options) : ILedgerFileImporter
{
    public async Task<LedgerImportSummary> ImportAsync(
        Stream ledger,
        ILedgerQuarantine quarantine,
        CancellationToken cancellationToken)
    {
        var batchSize = options.Value.BatchSize;
        var batch = new List<Lease>(batchSize);
        int leases = 0, months = 0, rejected = 0;

        foreach (var group in LedgerLeaseGrouper.Group(LedgerFileReader.Read(ledger)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (group.Rejected is { } rejectedRecord)
            {
                quarantine.Add(rejectedRecord);
                rejected++;
                continue;
            }

            var mapped = LedgerLeaseMapper.Map(group.Lease!);
            foreach (var duplicate in mapped.Rejected)
            {
                quarantine.Add(duplicate);
                rejected++;
            }

            batch.Add(mapped.Lease);
            leases++;
            months += mapped.Lease.ProductionMonths.Count;

            if (batch.Count >= batchSize)
            {
                await SaveBatchAsync(batch, cancellationToken);
            }
        }

        if (batch.Count > 0)
        {
            await SaveBatchAsync(batch, cancellationToken);
        }

        return new LedgerImportSummary(leases, months, rejected);
    }

    private async Task SaveBatchAsync(List<Lease> batch, CancellationToken cancellationToken)
    {
        var leaseNumbers = batch.Select(l => l.LeaseNumber).Distinct().ToList();
        var existing = await db.Leases
            .Include(l => l.ProductionMonths)
            .Where(l => leaseNumbers.Contains(l.LeaseNumber))
            .ToListAsync(cancellationToken);

        var leasesByKey = existing.ToDictionary(Key);

        foreach (var incoming in batch)
        {
            if (leasesByKey.TryGetValue(Key(incoming), out var current))
            {
                MergeMonths(current, incoming);
            }
            else
            {
                db.Leases.Add(incoming);
                leasesByKey.Add(Key(incoming), incoming);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
        batch.Clear();
    }

    private static void MergeMonths(Lease current, Lease incoming)
    {
        var monthsByDate = current.ProductionMonths.ToDictionary(m => m.Month);

        foreach (var month in incoming.ProductionMonths)
        {
            if (monthsByDate.TryGetValue(month.Month, out var existing))
            {
                existing.OperatorNumber = month.OperatorNumber;
                existing.OilBarrels = month.OilBarrels;
                existing.GasMcf = month.GasMcf;
                existing.CondensateBarrels = month.CondensateBarrels;
                existing.CasingheadGasMcf = month.CasingheadGasMcf;
            }
            else
            {
                current.ProductionMonths.Add(month);
                monthsByDate.Add(month.Month, month);
            }
        }
    }

    private static (string District, string Number, WellType WellType) Key(Lease lease) =>
        (lease.DistrictCode, lease.LeaseNumber, lease.WellType);
}
