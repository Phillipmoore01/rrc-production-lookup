namespace Rrc.Production.Data;

public class LeaseProductionMonth
{
    public int LeaseId { get; set; }

    public Lease Lease { get; set; } = null!;

    public DateOnly Month { get; set; }

    public decimal OilBarrels { get; set; }

    public decimal GasMcf { get; set; }

    public decimal CondensateBarrels { get; set; }

    public decimal CasingheadGasMcf { get; set; }
}
