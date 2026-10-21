using System.Globalization;

namespace Rrc.Ebcdic.Tests;

public class ZonedDecimalTests
{
    [Theory]
    [InlineData(new byte[] { 0xF1, 0xF2, 0xF3 }, 0, "123")]
    [InlineData(new byte[] { 0xF1, 0xF2, 0xC3 }, 0, "123")]
    [InlineData(new byte[] { 0xF1, 0xF2, 0xD3 }, 0, "-123")]
    [InlineData(new byte[] { 0xF0, 0xF4, 0xF5, 0xF0 }, 2, "4.50")]
    public void Decodes_signed_values_with_scale(byte[] field, int scale, string expected)
    {
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), ZonedDecimal.Decode(field, scale));
    }

    [Theory]
    [InlineData(new byte[] { 0x40, 0xF1 })]
    [InlineData(new byte[] { 0xF1, 0xFA })]
    [InlineData(new byte[] { 0xF1, 0x31 })]
    [InlineData(new byte[] { })]
    public void Rejects_malformed_fields(byte[] field)
    {
        Assert.Throws<FormatException>(() => ZonedDecimal.Decode(field));
    }
}
