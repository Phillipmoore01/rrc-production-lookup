using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Rrc.Production.Ingestion.Tests;

public class LedgerImportWorkerTests
{
    [Fact]
    public async Task Stops_the_host_after_scanning_the_input_directory()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            await File.WriteAllBytesAsync(Path.Combine(directory.FullName, "olf001.ebc"), [0x40, 0x40]);
            var lifetime = new Mock<IHostApplicationLifetime>();
            var worker = CreateWorker(directory.FullName, lifetime.Object);

            await worker.StartAsync(CancellationToken.None);
            await worker.ExecuteTask!;

            lifetime.Verify(l => l.StopApplication(), Times.Once);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Fails_and_stops_the_host_when_the_input_directory_is_missing()
    {
        var lifetime = new Mock<IHostApplicationLifetime>();
        var worker = CreateWorker(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), lifetime.Object);

        await worker.StartAsync(CancellationToken.None);
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => worker.ExecuteTask!);

        lifetime.Verify(l => l.StopApplication(), Times.Once);
    }

    private static LedgerImportWorker CreateWorker(string inputDirectory, IHostApplicationLifetime lifetime) =>
        new(
            Options.Create(new LedgerImportOptions { InputDirectory = inputDirectory }),
            lifetime,
            NullLogger<LedgerImportWorker>.Instance);
}
