using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rrc.Production.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Leases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DistrictCode = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    LeaseNumber = table.Column<string>(type: "varchar(6)", unicode: false, maxLength: 6, nullable: false),
                    WellType = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    LeaseName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    OperatorName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CountyName = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductionMonths",
                columns: table => new
                {
                    LeaseId = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<DateOnly>(type: "date", nullable: false),
                    OperatorNumber = table.Column<int>(type: "int", nullable: false),
                    OilBarrels = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    GasMcf = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    CondensateBarrels = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    CasingheadGasMcf = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionMonths", x => new { x.LeaseId, x.Month });
                    table.ForeignKey(
                        name: "FK_ProductionMonths_Leases_LeaseId",
                        column: x => x.LeaseId,
                        principalTable: "Leases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Leases_CountyName",
                table: "Leases",
                column: "CountyName");

            migrationBuilder.CreateIndex(
                name: "IX_Leases_DistrictCode_LeaseNumber_WellType",
                table: "Leases",
                columns: new[] { "DistrictCode", "LeaseNumber", "WellType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leases_OperatorName",
                table: "Leases",
                column: "OperatorName");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionMonths_OperatorNumber",
                table: "ProductionMonths",
                column: "OperatorNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductionMonths");

            migrationBuilder.DropTable(
                name: "Leases");
        }
    }
}
