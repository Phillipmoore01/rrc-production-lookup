using System.Globalization;

namespace Rrc.Ebcdic.Tests;

public class PackedDecimalTests
{
    [Theory]
    [InlineData(new byte[] { 0x12, 0x34, 0x5C }, 0, "12345")]
    [InlineData(new byte[] { 0x12, 0x34, 0x5D }, 0, "-12345")]
    [InlineData(new byte[] { 0x12, 0x34, 0x5F }, 0, "12345")]
    [InlineData(new byte[] { 0x0C }, 0, "0")]
    [InlineData(new byte[] { 0x01, 0x23, 0x45, 0x6C }, 2, "1234.56")]
    [InlineData(new byte[] { 0x00, 0x00, 0x5D }, 3, "-0.005")]
    public void Decodes_signed_values_with_scale(byte[] field, int scale, string expected)
    {
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), PackedDecimal.Decode(field, scale));
    }

    [Theory]
    [InlineData(new byte[] { 0x1A, 0x2C })]
    [InlineData(new byte[] { 0x12, 0x31 })]
    [InlineData(new byte[] { })]
    public void Rejects_malformed_fields(byte[] field)
    {
        Assert.Throws<FormatException>(() => PackedDecimal.Decode(field));
    }
}
