namespace Rrc.Ledger.Tests;

public class CycleKeyTests
{
    [Theory]
    [InlineData(9927, 1993, 1)]
    [InlineData(9926, 1993, 2)]
    [InlineData(9916, 1993, 12)]
    [InlineData(9915, 1994, 1)]
    [InlineData(9999, 1987, 1)]
    public void Maps_cycle_keys_to_months(int key, int year, int month)
    {
        Assert.Equal(new DateOnly(year, month, 1), CycleKey.ToMonth(key));
        Assert.Equal(key, CycleKey.FromMonth(new DateOnly(year, month, 1)));
    }

    [Fact]
    public void Rejects_months_before_the_cycle_key_epoch()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CycleKey.FromMonth(new DateOnly(1986, 12, 1)));
    }
}
