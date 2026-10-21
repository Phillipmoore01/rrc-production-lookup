using System.Text;

namespace Rrc.Ebcdic;

public static class EbcdicText
{
    private const int UsCanadaCodePage = 37;

    private static readonly Encoding Encoding = CreateEncoding();

    public static string Decode(ReadOnlySpan<byte> field) => Encoding.GetString(field).TrimEnd();

    private static Encoding CreateEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(UsCanadaCodePage);
    }
}
