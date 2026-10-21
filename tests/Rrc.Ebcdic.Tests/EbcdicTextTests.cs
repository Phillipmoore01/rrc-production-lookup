namespace Rrc.Ebcdic.Tests;

public class EbcdicTextTests
{
    [Fact]
    public void Decodes_code_page_037_and_trims_padding()
    {
        byte[] field = [0xC1, 0xC3, 0xD4, 0xC5, 0x40, 0xF1, 0xF2, 0x40, 0x40];

        Assert.Equal("ACME 12", EbcdicText.Decode(field));
    }

    [Fact]
    public void Decodes_all_spaces_as_empty()
    {
        byte[] field = [0x40, 0x40, 0x40];

        Assert.Equal(string.Empty, EbcdicText.Decode(field));
    }
}
