using System.Globalization;
using static Rrc.Ledger.LedgerField;

namespace Rrc.Ledger;

public static class LedgerRecordParser
{
    public const int RecordLength = 160;

    private const byte EbcdicZero = 0xF0;
    private const int TrailerZerosStart = 2;
    private const int TrailerZerosLength = 30;

    public static LedgerRecord Parse(ReadOnlySpan<byte> record)
    {
        if (record.Length != RecordLength)
        {
            throw new LedgerFormatException($"Ledger record is {record.Length} bytes; expected {RecordLength}.");
        }

        var recordId = Text(record, 1, 2);
        if (!int.TryParse(recordId, NumberStyles.None, CultureInfo.InvariantCulture, out var typeValue)
            || !Enum.IsDefined((LedgerRecordType)typeValue))
        {
            throw new LedgerFormatException($"Unknown ledger record id '{recordId}'.");
        }

        return (LedgerRecordType)typeValue switch
        {
            LedgerRecordType.Root => ParseRoot(record),
            LedgerRecordType.GasCycle => ParseGasCycle(record),
            LedgerRecordType.GasBalancingPeriod => ParseGasBalancingPeriod(record),
            LedgerRecordType.OilCycle => ParseOilCycle(record),
            var dispositionType => new Disposition(dispositionType, Number(record, 3, 2), Packed(record, 5)),
        };
    }

    private static LedgerRecord ParseRoot(ReadOnlySpan<byte> record)
    {
        if (IsTrailer(record))
        {
            return new LedgerTrailer();
        }

        var district = RrcDistrict.FromTapeValue(Number(record, 4, 2));
        var number = Number(record, 6, 6);

        return Character(record, 3) switch
        {
            'O' => new OilLeaseRoot(
                district,
                number,
                MovableBalance: Packed(record, 12),
                BeginningOilStatus: Packed(record, 17),
                BeginningCasingheadStatus: Packed(record, 22),
                OldestEndOfMonthBalance: Packed(record, 27)),
            'G' => new GasWellRoot(
                district,
                number,
                GasOldestEndOfMonthBalance: Packed(record, 12),
                CondensateOldestEndOfMonthBalance: Packed(record, 17),
                LiquidOldestEndOfMonthBalance: Packed(record, 22)),
            var code => throw new LedgerFormatException($"Unknown root oil/gas code '{code}'."),
        };
    }

    private static GasCycle ParseGasCycle(ReadOnlySpan<byte> record) => new(
        Month: CycleKey.ToMonth(Number(record, 3, 4)),
        OperatorNumber: Number(record, 7, 6),
        FieldNumber: Number(record, 13, 5),
        ReservoirNumber: Number(record, 18, 3),
        TypeFieldCode: Text(record, 22, 2),
        WellNumber: Text(record, 26, 6),
        P2Filed: IsYes(record, 40),
        CorrectedP2: IsYes(record, 41),
        Allowable: Packed(record, 50),
        WellType: Text(record, 55, 2),
        Commingled: IsYes(record, 69),
        GasProductionMcf: Packed(record, 70),
        LiftGasInjectedMcf: Packed(record, 81),
        CondensateLimit: Packed(record, 114),
        CondensatePlantProductionBarrels: Packed(record, 119),
        CondensateLeaseProductionBarrels: Packed(record, 124),
        CondensateEndOfMonthBalance: Packed(record, 129));

    private static OilCycle ParseOilCycle(ReadOnlySpan<byte> record) => new(
        Month: CycleKey.ToMonth(Number(record, 3, 4)),
        OperatorNumber: Number(record, 7, 6),
        FieldNumber: Number(record, 13, 5),
        ReservoirNumber: Number(record, 18, 3),
        TypeFieldCode: Character(record, 21),
        P1Filed: IsYes(record, 22),
        CorrectedP1: IsYes(record, 23),
        FlowingWells: Number(record, 24, 3),
        OtherWells: Number(record, 27, 3),
        Commingled: IsYes(record, 30),
        Allowable: Packed(record, 31),
        OilProductionBarrels: Packed(record, 36),
        OilEndingBalance: Packed(record, 41),
        OilPresentStatus: Packed(record, 46),
        OilMovableBalance: Packed(record, 51),
        CasingheadLimit: Packed(record, 56),
        CasingheadProductionMcf: Packed(record, 61),
        CasingheadLiftMcf: Packed(record, 66),
        CasingheadStatus: Packed(record, 71));

    private static GasBalancingPeriod ParseGasBalancingPeriod(ReadOnlySpan<byte> record) => new(
        BalancingPeriodStatus: Packed(record, 3),
        CancelledUnderage: Packed(record, 8),
        OffFileBalancingPeriodStatus: Packed(record, 13),
        OffFileCumulativeOverage: Packed(record, 18));

    private static bool IsTrailer(ReadOnlySpan<byte> record) =>
        !record.Slice(TrailerZerosStart, TrailerZerosLength).ContainsAnyExcept(EbcdicZero);

    private static bool IsYes(ReadOnlySpan<byte> record, int position) =>
        char.ToUpperInvariant(Character(record, position)) == 'Y';
}
