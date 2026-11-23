namespace Rrc.Ledger;

public enum LedgerRecordType
{
    Root = 1,
    GasCycle = 2,
    GasDisposition = 3,
    CondensateDisposition = 4,
    GasBalancingPeriod = 5,
    OilCycle = 6,
    OilDisposition = 7,
    CasingheadDisposition = 8,
}

public abstract record LedgerRecord(LedgerRecordType Type);

public sealed record OilLeaseRoot(
    string District,
    int LeaseNumber,
    long MovableBalance,
    long BeginningOilStatus,
    long BeginningCasingheadStatus,
    long OldestEndOfMonthBalance) : LedgerRecord(LedgerRecordType.Root);

public sealed record LedgerTrailer() : LedgerRecord(LedgerRecordType.Root);

public sealed record GasWellRoot(
    string District,
    int RrcId,
    long GasOldestEndOfMonthBalance,
    long CondensateOldestEndOfMonthBalance,
    long LiquidOldestEndOfMonthBalance) : LedgerRecord(LedgerRecordType.Root);

public sealed record GasCycle(
    DateOnly Month,
    int OperatorNumber,
    int FieldNumber,
    int ReservoirNumber,
    string TypeFieldCode,
    string WellNumber,
    bool P2Filed,
    bool CorrectedP2,
    long Allowable,
    string WellType,
    bool Commingled,
    long GasProductionMcf,
    long LiftGasInjectedMcf,
    long CondensateLimit,
    long CondensatePlantProductionBarrels,
    long CondensateLeaseProductionBarrels,
    long CondensateEndOfMonthBalance) : LedgerRecord(LedgerRecordType.GasCycle);

public sealed record OilCycle(
    DateOnly Month,
    int OperatorNumber,
    int FieldNumber,
    int ReservoirNumber,
    char TypeFieldCode,
    bool P1Filed,
    bool CorrectedP1,
    int FlowingWells,
    int OtherWells,
    bool Commingled,
    long Allowable,
    long OilProductionBarrels,
    long OilEndingBalance,
    long OilPresentStatus,
    long OilMovableBalance,
    long CasingheadLimit,
    long CasingheadProductionMcf,
    long CasingheadLiftMcf,
    long CasingheadStatus) : LedgerRecord(LedgerRecordType.OilCycle);

public sealed record Disposition(LedgerRecordType Type, int Code, long Amount) : LedgerRecord(Type);

public sealed record GasBalancingPeriod(
    long BalancingPeriodStatus,
    long CancelledUnderage,
    long OffFileBalancingPeriodStatus,
    long OffFileCumulativeOverage) : LedgerRecord(LedgerRecordType.GasBalancingPeriod);
