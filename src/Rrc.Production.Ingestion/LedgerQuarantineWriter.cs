using System.Globalization;
using Rrc.Ledger;

namespace Rrc.Production.Ingestion;

public interface ILedgerQuarantine
{
    void Add(LedgerReadResult rejected);
}

public sealed class LedgerQuarantineWriter(string directory, string sourceFileName) : ILedgerQuarantine, IDisposable
{
    private FileStream? _records;
    private StreamWriter? _log;

    public string RecordsPath => Path.Combine(directory, sourceFileName + ".rejected");

    public string LogPath => Path.Combine(directory, sourceFileName + ".rejected.log");

    public int Count { get; private set; }

    public void Add(LedgerReadResult rejected)
    {
        if (_records is null || _log is null)
        {
            Directory.CreateDirectory(directory);
            _records = new FileStream(RecordsPath, FileMode.Create, FileAccess.Write);
            _log = new StreamWriter(LogPath, append: false);
        }

        _records.Write(rejected.RawBytes);
        _log.WriteLine(string.Create(CultureInfo.InvariantCulture, $"{rejected.RecordNumber}\t{rejected.Error}"));
        Count++;
    }

    public void Dispose()
    {
        _log?.Dispose();
        _records?.Dispose();
    }
}
