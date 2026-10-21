namespace Rrc.Production.Ingestion;

public class LedgerImportOptions
{
    public const string SectionName = "LedgerImport";

    public string InputDirectory { get; set; } = "ledgers";

    public string FilePattern { get; set; } = "*";
}
