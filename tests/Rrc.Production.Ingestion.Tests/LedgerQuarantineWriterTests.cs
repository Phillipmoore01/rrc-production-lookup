using Rrc.Ledger;

namespace Rrc.Production.Ingestion.Tests;

public class LedgerQuarantineWriterTests
{
    [Fact]
    public void Writes_raw_records_and_a_log_line_per_rejection()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        try
        {
            string recordsPath, logPath;
            using (var writer = new LedgerQuarantineWriter(directory, "olf008.ebc"))
            {
                writer.Add(LedgerReadResult.Invalid(3, [0x01, 0x02], "Bad field."));
                writer.Add(LedgerReadResult.Invalid(9, [0x03], "Orphan."));
                (recordsPath, logPath) = (writer.RecordsPath, writer.LogPath);
                Assert.Equal(2, writer.Count);
            }

            Assert.Equal([0x01, 0x02, 0x03], File.ReadAllBytes(recordsPath));
            Assert.Equal(["3\tBad field.", "9\tOrphan."], File.ReadAllLines(logPath));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void Creates_no_files_when_nothing_is_rejected()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        using (new LedgerQuarantineWriter(directory, "olf008.ebc"))
        {
        }

        Assert.False(Directory.Exists(directory));
    }
}
