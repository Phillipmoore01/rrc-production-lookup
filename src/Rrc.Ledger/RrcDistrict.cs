namespace Rrc.Ledger;

public static class RrcDistrict
{
    private static readonly string[] DistrictIdsByTapeValue =
        ["01", "02", "03", "04", "05", "06", "6E", "7B", "7C", "08", "8A", "8B", "09", "10"];

    public static string FromTapeValue(int tapeValue) =>
        tapeValue is >= 1 and <= 14
            ? DistrictIdsByTapeValue[tapeValue - 1]
            : throw new LedgerFormatException($"District tape value {tapeValue} is not in the range 1-14.");
}
