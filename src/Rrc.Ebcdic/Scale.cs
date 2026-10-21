namespace Rrc.Ebcdic;

internal static class Scale
{
    private const int MaxDecimalScale = 28;

    public static decimal Apply(decimal value, int scale)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(scale);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(scale, MaxDecimalScale);

        for (var i = 0; i < scale; i++)
        {
            value /= 10m;
        }

        return value;
    }
}
