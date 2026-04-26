using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KhanLogistics.Migrations
{
    public partial class AddExNoToDispatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "TblDispatches");

            migrationBuilder.AddColumn<string>(
                name: "ExNo",
                table: "TblDispatches",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExNo",
                table: "TblDispatches");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "TblDispatches",
                type: "datetime2",
                nullable: true);
        }
    }
}
