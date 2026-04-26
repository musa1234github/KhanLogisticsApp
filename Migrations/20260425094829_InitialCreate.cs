using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhanLogistics.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BillDetailViewModels",
                columns: table => new
                {
                    FID = table.Column<int>(type: "int", nullable: false),
                    BillDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BillNum = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillQty = table.Column<double>(type: "float", nullable: true),
                    TaxableAmount = table.Column<double>(type: "float", nullable: true),
                    ActualAmount = table.Column<double>(type: "float", nullable: true),
                    Tds = table.Column<double>(type: "float", nullable: true),
                    Gst = table.Column<double>(type: "float", nullable: true),
                    LrQty = table.Column<int>(type: "int", nullable: true),
                    FactoryName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DispatchMonth = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "PaymentTables",
                columns: table => new
                {
                    PId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayRecDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FID = table.Column<int>(type: "int", nullable: true),
                    DocNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Shortage = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTables", x => x.PId);
                });

            migrationBuilder.CreateTable(
                name: "Sp_QtyByDay",
                columns: table => new
                {
                    Factory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    totalQty = table.Column<double>(type: "float", nullable: true),
                    BillQty = table.Column<double>(type: "float", nullable: true),
                    DayName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "Sp_QtyByMonth",
                columns: table => new
                {
                    Factory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    totalQty = table.Column<double>(type: "float", nullable: true),
                    BillQty = table.Column<double>(type: "float", nullable: true),
                    MonthName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Balance = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "SpBilldetails",
                columns: table => new
                {
                    FID = table.Column<int>(type: "int", nullable: false),
                    BillDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BillNum = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillQty = table.Column<double>(type: "float", nullable: true),
                    TaxableAmount = table.Column<double>(type: "float", nullable: true),
                    ActualAmount = table.Column<double>(type: "float", nullable: true),
                    Tds = table.Column<double>(type: "float", nullable: true),
                    Gst = table.Column<double>(type: "float", nullable: true),
                    LrQty = table.Column<int>(type: "int", nullable: true),
                    FactoryName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DispatchMonth = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "SpCheckBill",
                columns: table => new
                {
                    ChallanNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DispatchDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DispatchQuantity = table.Column<double>(type: "float", nullable: true),
                    FactoryName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Destination = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VehicleNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillNum = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "SpCheckPaymentDetails",
                columns: table => new
                {
                    BillNum = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentNum = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Deduction = table.Column<double>(type: "float", nullable: true),
                    PaymentReceived = table.Column<double>(type: "float", nullable: true),
                    ActualAmount = table.Column<double>(type: "float", nullable: true),
                    Tds = table.Column<double>(type: "float", nullable: true),
                    Gst = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "SpExpoBilldetail",
                columns: table => new
                {
                    BillNum = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BillDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BillType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FactoryId = table.Column<int>(type: "int", nullable: false),
                    FactoryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BillQty = table.Column<double>(type: "float", nullable: true),
                    TaxableAmount = table.Column<double>(type: "float", nullable: true),
                    FinalPrice = table.Column<double>(type: "float", nullable: true),
                    ActualAmount = table.Column<double>(type: "float", nullable: true),
                    Tds = table.Column<double>(type: "float", nullable: true),
                    Gst = table.Column<double>(type: "float", nullable: true),
                    DispatchMonth = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LrQty = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "SpGetDispatches",
                columns: table => new
                {
                    DispId = table.Column<int>(type: "int", nullable: false),
                    ChallanNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DispatchDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DispatchQuantity = table.Column<double>(type: "float", nullable: true),
                    UnitPrice = table.Column<double>(type: "float", nullable: true),
                    FactoryName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Destination = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VehicleNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillNum = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "SpOutStanding",
                columns: table => new
                {
                    BillNum = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BillType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FactoryName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActualAmount = table.Column<double>(type: "float", nullable: true),
                    DispatchMonth = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentReceived = table.Column<double>(type: "float", nullable: true),
                    InvoiceAge = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "SpPaymentByDate",
                columns: table => new
                {
                    PaymentNum = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalPaymentReceived = table.Column<double>(type: "float", nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Deduction = table.Column<double>(type: "float", nullable: true),
                    AmountAfterDeduction = table.Column<double>(type: "float", nullable: true),
                    FactoryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalActualAmount = table.Column<double>(type: "float", nullable: true),
                    Tds = table.Column<double>(type: "float", nullable: true),
                    Gst = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "TblFactories",
                columns: table => new
                {
                    FID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FactoryName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(type: "int", nullable: true),
                    Gstin = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblFactories", x => x.FID);
                });

            migrationBuilder.CreateTable(
                name: "TblFreights",
                columns: table => new
                {
                    DestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Destination = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Wheels = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FreightRate = table.Column<double>(type: "float", nullable: false),
                    Vid = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblFreights", x => x.DestId);
                });

            migrationBuilder.CreateTable(
                name: "TblUsers",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Doj = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLogIn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblUsers", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "TblVehicles",
                columns: table => new
                {
                    VehicleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VehicleType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VehicleInsurStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VehicleInsurEndtDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VehicleFitnessStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VehicleFitnessEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TaxStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TaxEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VehiclePermitDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblVehicles", x => x.VehicleId);
                });

            migrationBuilder.CreateTable(
                name: "BillTables",
                columns: table => new
                {
                    BillID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BillNum = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PId = table.Column<int>(type: "int", nullable: true),
                    BillDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GstDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BillType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FID = table.Column<int>(type: "int", nullable: true),
                    PaymentReceived = table.Column<double>(type: "float", nullable: true),
                    ActualAmount = table.Column<double>(type: "float", nullable: true),
                    Tds = table.Column<double>(type: "float", nullable: true),
                    Gst = table.Column<double>(type: "float", nullable: true),
                    PartyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalValue = table.Column<double>(type: "float", nullable: true),
                    PaymentTablePId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillTables", x => x.BillID);
                    table.ForeignKey(
                        name: "FK_BillTables_PaymentTables_PaymentTablePId",
                        column: x => x.PaymentTablePId,
                        principalTable: "PaymentTables",
                        principalColumn: "PId");
                });

            migrationBuilder.CreateTable(
                name: "TblDispatches",
                columns: table => new
                {
                    DispId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChallanNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DispatchDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Destination = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DispatchQuantity = table.Column<double>(type: "float", nullable: true),
                    UnitPrice = table.Column<double>(type: "float", nullable: true),
                    FinalPrice = table.Column<double>(type: "float", nullable: true),
                    DisVid = table.Column<int>(type: "int", nullable: true),
                    Shortage = table.Column<int>(type: "int", nullable: true),
                    BillID = table.Column<int>(type: "int", nullable: true),
                    VehicleNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PartyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Lr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeliveryNum = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalValue = table.Column<double>(type: "float", nullable: true),
                    IsReceived = table.Column<bool>(type: "bit", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblDispatches", x => x.DispId);
                    table.ForeignKey(
                        name: "FK_TblDispatches_BillTables_BillID",
                        column: x => x.BillID,
                        principalTable: "BillTables",
                        principalColumn: "BillID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillTables_PaymentTablePId",
                table: "BillTables",
                column: "PaymentTablePId");

            migrationBuilder.CreateIndex(
                name: "IX_TblDispatches_BillID",
                table: "TblDispatches",
                column: "BillID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillDetailViewModels");

            migrationBuilder.DropTable(
                name: "Sp_QtyByDay");

            migrationBuilder.DropTable(
                name: "Sp_QtyByMonth");

            migrationBuilder.DropTable(
                name: "SpBilldetails");

            migrationBuilder.DropTable(
                name: "SpCheckBill");

            migrationBuilder.DropTable(
                name: "SpCheckPaymentDetails");

            migrationBuilder.DropTable(
                name: "SpExpoBilldetail");

            migrationBuilder.DropTable(
                name: "SpGetDispatches");

            migrationBuilder.DropTable(
                name: "SpOutStanding");

            migrationBuilder.DropTable(
                name: "SpPaymentByDate");

            migrationBuilder.DropTable(
                name: "TblDispatches");

            migrationBuilder.DropTable(
                name: "TblFactories");

            migrationBuilder.DropTable(
                name: "TblFreights");

            migrationBuilder.DropTable(
                name: "TblUsers");

            migrationBuilder.DropTable(
                name: "TblVehicles");

            migrationBuilder.DropTable(
                name: "BillTables");

            migrationBuilder.DropTable(
                name: "PaymentTables");
        }
    }
}
