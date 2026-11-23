namespace Rrc.Ledger;

public class LedgerFormatException : FormatException
{
    public LedgerFormatException(string message)
        : base(message)
    {
    }

    public LedgerFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
