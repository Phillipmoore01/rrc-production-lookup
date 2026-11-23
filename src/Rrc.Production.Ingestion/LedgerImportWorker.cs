using System.IO.Compression;
using Microsoft.Extensions.Options;

namespace Rrc.Production.Ingestion;

public class LedgerImportWorker(
    IOptions<LedgerImportOptions> options,
    IServiceScopeFactory scopeFactory,
    IHostApplicationLifetime lifetime,
    ILogger<LedgerImportWorker> logger) : BackgroundService
{
    private const string GzipExtension = ".gz";
    private const int BufferSize = 1 << 20;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var settings = options.Value;
            var input = new DirectoryInfo(settings.InputDirectory);

            if (!input.Exists)
            {
                throw new DirectoryNotFoundException($"Ledger input directory '{input.FullName}' does not exist.");
            }

            foreach (var file in input.EnumerateFiles(settings.FilePattern).OrderBy(f => f.Name, StringComparer.Ordinal))
            {
                stoppingToken.ThrowIfCancellationRequested();
                await ImportFileAsync(file, settings.QuarantineDirectory, stoppingToken);
            }
        }
        finally
        {
            lifetime.StopApplication();
        }
    }

    private async Task ImportFileAsync(FileInfo file, string quarantineDirectory, CancellationToken stoppingToken)
    {
        logger.LogInformation("Importing ledger file {File} ({Bytes} bytes)", file.Name, file.Length);

        await using var scope = scopeFactory.CreateAsyncScope();
        var importer = scope.ServiceProvider.GetRequiredService<ILedgerFileImporter>();

        await using var stream = OpenLedger(file);
        using var quarantine = new LedgerQuarantineWriter(quarantineDirectory, LedgerName(file));

        var summary = await importer.ImportAsync(stream, quarantine, stoppingToken);

        logger.LogInformation(
            "Imported {File}: {Leases} leases, {Months} production months, {Rejected} rejected records",
            file.Name,
            summary.Leases,
            summary.Months,
            summary.Rejected);

        if (summary.Rejected > 0)
        {
            logger.LogWarning("Rejected records from {File} written to {Path}", file.Name, quarantine.LogPath);
        }
    }

    private static bool IsGzip(FileInfo file) => file.Extension.Equals(GzipExtension, StringComparison.OrdinalIgnoreCase);

    private static string LedgerName(FileInfo file) => IsGzip(file) ? file.Name[..^GzipExtension.Length] : file.Name;

    private static Stream OpenLedger(FileInfo file)
    {
        var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize);
        return IsGzip(file) ? new GZipStream(stream, CompressionMode.Decompress) : stream;
    }
}
