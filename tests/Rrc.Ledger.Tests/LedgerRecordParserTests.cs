namespace Rrc.Ledger.Tests;

public class LedgerRecordParserTests
{
    [Fact]
    public void Decodes_a_hand_encoded_oil_root_record()
    {
        var record = new byte[LedgerRecordParser.RecordLength];
        Array.Fill(record, (byte)0x40);
        byte[] prefix =
        [
            0xF0, 0xF1,
            0xD6,
            0xF0, 0xF8,
            0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5,
            0x00, 0x00, 0x01, 0x50, 0x0C,
            0x00, 0x00, 0x00, 0x00, 0x0C,
            0x00, 0x00, 0x00, 0x04, 0x2D,
            0x00, 0x00, 0x00, 0x99, 0x9C,
        ];
        prefix.CopyTo(record, 0);

        var root = Assert.IsType<OilLeaseRoot>(LedgerRecordParser.Parse(record));

        Assert.Equal(new OilLeaseRoot("7B", 12345, 1500, 0, -42, 999), root);
    }

    [Fact]
    public void Decodes_a_gas_root_record()
    {
        var record = new LedgerRecordBuilder("01")
            .Text(3, "G")
            .Zoned(4, 2, 11)
            .Zoned(6, 6, 204417)
            .Packed(12, 75)
            .Packed(17, -3)
            .Packed(22, 0)
            .Build();

        Assert.Equal(new GasWellRoot("8A", 204417, 75, -3, 0), LedgerRecordParser.Parse(record));
    }

    [Fact]
    public void Decodes_a_gas_cycle_record()
    {
        var record = new LedgerRecordBuilder("02")
            .Zoned(3, 4, 9927)
            .Zoned(7, 6, 123456)
            .Zoned(13, 5, 48263)
            .Zoned(18, 3, 1)
            .Text(21, "N")
            .Text(22, "PR")
            .Text(26, "1H")
            .Zoned(32, 8, 0)
            .Text(40, "Y")
            .Text(41, "N")
            .Packed(50, 30000)
            .Text(55, "G ")
            .Packed(62, 0)
            .Text(69, "N")
            .Packed(70, 28765)
            .Packed(76, 0)
            .Packed(81, 120)
            .Packed(86, 0)
            .Packed(91, 0)
            .Packed(104, 0)
            .Packed(109, 0)
            .Packed(114, 900)
            .Packed(119, 15)
            .Packed(124, 312)
            .Packed(129, -7)
            .Packed(135, 0)
            .Packed(140, 0)
            .Build();

        var expected = new GasCycle(
            Month: new DateOnly(1993, 1, 1),
            OperatorNumber: 123456,
            FieldNumber: 48263,
            ReservoirNumber: 1,
            TypeFieldCode: "PR",
            WellNumber: "1H",
            P2Filed: true,
            CorrectedP2: false,
            Allowable: 30000,
            WellType: "G",
            Commingled: false,
            GasProductionMcf: 28765,
            LiftGasInjectedMcf: 120,
            CondensateLimit: 900,
            CondensatePlantProductionBarrels: 15,
            CondensateLeaseProductionBarrels: 312,
            CondensateEndOfMonthBalance: -7);

        Assert.Equal(expected, LedgerRecordParser.Parse(record));
    }

    [Fact]
    public void Decodes_an_oil_cycle_record()
    {
        var record = new LedgerRecordBuilder("06")
            .Zoned(3, 4, 9926)
            .Zoned(7, 6, 654321)
            .Zoned(13, 5, 10125)
            .Zoned(18, 3, 2)
            .Text(21, "R")
            .Text(22, "Y")
            .Text(23, "Y")
            .Zoned(24, 3, 4)
            .Zoned(27, 3, 2)
            .Text(30, "N")
            .Packed(31, 3100)
            .Packed(36, 2875)
            .Packed(41, 140)
            .Packed(46, -25)
            .Packed(51, 115)
            .Packed(56, 6200)
            .Packed(61, 4410)
            .Packed(66, 0)
            .Packed(71, 0)
            .Build();

        var expected = new OilCycle(
            Month: new DateOnly(1993, 2, 1),
            OperatorNumber: 654321,
            FieldNumber: 10125,
            ReservoirNumber: 2,
            TypeFieldCode: 'R',
            P1Filed: true,
            CorrectedP1: true,
            FlowingWells: 4,
            OtherWells: 2,
            Commingled: false,
            Allowable: 3100,
            OilProductionBarrels: 2875,
            OilEndingBalance: 140,
            OilPresentStatus: -25,
            OilMovableBalance: 115,
            CasingheadLimit: 6200,
            CasingheadProductionMcf: 4410,
            CasingheadLiftMcf: 0,
            CasingheadStatus: 0);

        Assert.Equal(expected, LedgerRecordParser.Parse(record));
    }

    [Theory]
    [InlineData("03", LedgerRecordType.GasDisposition)]
    [InlineData("04", LedgerRecordType.CondensateDisposition)]
    [InlineData("07", LedgerRecordType.OilDisposition)]
    [InlineData("08", LedgerRecordType.CasingheadDisposition)]
    public void Decodes_disposition_records(string recordId, LedgerRecordType expectedType)
    {
        var record = new LedgerRecordBuilder(recordId).Zoned(3, 2, 2).Packed(5, 2650).Build();

        Assert.Equal(new Disposition(expectedType, 2, 2650), LedgerRecordParser.Parse(record));
    }

    [Fact]
    public void Decodes_a_gas_balancing_period_record()
    {
        var record = new LedgerRecordBuilder("05")
            .Packed(3, 100)
            .Packed(8, 200)
            .Packed(13, -300)
            .Packed(18, 400)
            .Build();

        Assert.Equal(new GasBalancingPeriod(100, 200, -300, 400), LedgerRecordParser.Parse(record));
    }

    [Fact]
    public void Rejects_records_of_the_wrong_length()
    {
        Assert.Throws<LedgerFormatException>(() => LedgerRecordParser.Parse(new byte[159]));
    }

    [Theory]
    [InlineData("00")]
    [InlineData("09")]
    [InlineData("AB")]
    [InlineData("  ")]
    public void Rejects_unknown_record_ids(string recordId)
    {
        Assert.Throws<LedgerFormatException>(() => LedgerRecordParser.Parse(new LedgerRecordBuilder(recordId).Build()));
    }

    [Fact]
    public void Rejects_a_root_with_an_unknown_oil_gas_code()
    {
        var record = new LedgerRecordBuilder("01").Text(3, "X").Zoned(4, 2, 1).Zoned(6, 6, 1).Build();

        Assert.Throws<LedgerFormatException>(() => LedgerRecordParser.Parse(record));
    }

    [Fact]
    public void Rejects_a_root_with_an_out_of_range_district()
    {
        var record = new LedgerRecordBuilder("01").Text(3, "O").Zoned(4, 2, 15).Zoned(6, 6, 1).Build();

        Assert.Throws<LedgerFormatException>(() => LedgerRecordParser.Parse(record));
    }

    [Fact]
    public void Reports_the_position_of_a_malformed_packed_field()
    {
        var record = new LedgerRecordBuilder("06")
            .Zoned(3, 4, 9927)
            .Zoned(7, 6, 1)
            .Zoned(13, 5, 1)
            .Zoned(18, 3, 1)
            .Zoned(24, 3, 0)
            .Zoned(27, 3, 0)
            .Packed(31, 0)
            .Raw(36, 0x00, 0x00, 0x00, 0x00, 0x00)
            .Build();

        var error = Assert.Throws<LedgerFormatException>(() => LedgerRecordParser.Parse(record));
        Assert.Contains("position 36", error.Message);
    }
}
