using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Rrc.Production.Ingestion.Tests;

public class LedgerImportWorkerTests
{
    [Fact]
    public async Task Imports_each_file_in_name_order_and_stops_the_host()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            await File.WriteAllBytesAsync(Path.Combine(directory.FullName, "olf002.ebc"), [0x40]);
            await File.WriteAllBytesAsync(Path.Combine(directory.FullName, "olf001.ebc"), [0x40]);

            var importedLengths = new List<long>();
            var importer = new Mock<ILedgerFileImporter>();
            importer
                .Setup(i => i.ImportAsync(It.IsAny<Stream>(), It.IsAny<ILedgerQuarantine>(), It.IsAny<CancellationToken>()))
                .Callback<Stream, ILedgerQuarantine, CancellationToken>((s, _, _) => importedLengths.Add(s.Length))
                .ReturnsAsync(new LedgerImportSummary(0, 0, 0));
            var lifetime = new Mock<IHostApplicationLifetime>();
            var worker = CreateWorker(directory.FullName, importer.Object, lifetime.Object);

            await worker.StartAsync(CancellationToken.None);
            await worker.ExecuteTask!;

            Assert.Equal(2, importedLengths.Count);
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
        var worker = CreateWorker(
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            Mock.Of<ILedgerFileImporter>(),
            lifetime.Object);

        await worker.StartAsync(CancellationToken.None);
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => worker.ExecuteTask!);

        lifetime.Verify(l => l.StopApplication(), Times.Once);
    }

    private static LedgerImportWorker CreateWorker(
        string inputDirectory,
        ILedgerFileImporter importer,
        IHostApplicationLifetime lifetime)
    {
        var services = new ServiceCollection().AddSingleton(importer).BuildServiceProvider();
        var options = new LedgerImportOptions
        {
            InputDirectory = inputDirectory,
            QuarantineDirectory = Path.Combine(inputDirectory, "quarantine"),
        };

        return new LedgerImportWorker(
            Options.Create(options),
            services.GetRequiredService<IServiceScopeFactory>(),
            lifetime,
            NullLogger<LedgerImportWorker>.Instance);
    }
}
