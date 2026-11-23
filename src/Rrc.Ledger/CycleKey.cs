namespace Rrc.Ledger;

public static class CycleKey
{
    private const int MaxKey = 9999;

    private static readonly DateOnly EpochMonth = new(1987, 1, 1);

    public static DateOnly ToMonth(int key)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(key);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(key, MaxKey);

        return EpochMonth.AddMonths(MaxKey - key);
    }

    public static int FromMonth(DateOnly month)
    {
        var monthsSinceEpoch = (month.Year - EpochMonth.Year) * 12 + month.Month - EpochMonth.Month;
        var key = MaxKey - monthsSinceEpoch;

        return key is >= 0 and <= MaxKey
            ? key
            : throw new ArgumentOutOfRangeException(nameof(month), month, "Month is outside the ledger cycle key range.");
    }
}
