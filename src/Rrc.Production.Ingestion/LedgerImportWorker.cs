using Microsoft.Extensions.Options;

namespace Rrc.Production.Ingestion;

public class LedgerImportWorker(
    IOptions<LedgerImportOptions> options,
    IHostApplicationLifetime lifetime,
    ILogger<LedgerImportWorker> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var settings = options.Value;
            var input = new DirectoryInfo(settings.InputDirectory);

            if (!input.Exists)
            {
                throw new DirectoryNotFoundException($"Ledger input directory '{input.FullName}' does not exist.");
            }

            foreach (var file in input.EnumerateFiles(settings.FilePattern))
            {
                stoppingToken.ThrowIfCancellationRequested();
                logger.LogInformation("Found ledger file {File} ({Bytes} bytes)", file.Name, file.Length);
            }

            return Task.CompletedTask;
        }
        finally
        {
            lifetime.StopApplication();
        }
    }
}
