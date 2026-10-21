namespace Rrc.Production.Data;

public class Lease
{
    public int Id { get; set; }

    public required string DistrictCode { get; set; }

    public required string LeaseNumber { get; set; }

    public required WellType WellType { get; set; }

    public string? LeaseName { get; set; }

    public string? OperatorName { get; set; }

    public string? CountyName { get; set; }

    public List<LeaseProductionMonth> ProductionMonths { get; } = [];
}

public enum WellType
{
    Oil,
    Gas,
}
