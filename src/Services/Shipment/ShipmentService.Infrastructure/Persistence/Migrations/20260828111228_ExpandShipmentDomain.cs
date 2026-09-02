using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipmentService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandShipmentDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PickupAddress",
                table: "Shipments",
                newName: "PickupAddress_Street");

            migrationBuilder.RenameColumn(
                name: "DeliveryAddress",
                table: "Shipments",
                newName: "PickupAddress_State");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "StatusHistories",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Shipments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<decimal>(
                name: "Cost",
                table: "Shipments",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Shipments",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddress_City",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddress_Country",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "DeliveryAddress_Latitude",
                table: "Shipments",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DeliveryAddress_Longitude",
                table: "Shipments",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddress_PostalCode",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddress_State",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeliveryAddress_Street",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PickupAddress_City",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PickupAddress_Country",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "PickupAddress_Latitude",
                table: "Shipments",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PickupAddress_Longitude",
                table: "Shipments",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickupAddress_PostalCode",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DeliveryAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Successful = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttemptedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryAttempts", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryAttempts");

            migrationBuilder.DropColumn(
                name: "Cost",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress_City",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress_Country",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress_Latitude",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress_Longitude",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress_PostalCode",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress_State",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DeliveryAddress_Street",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "PickupAddress_City",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "PickupAddress_Country",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "PickupAddress_Latitude",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "PickupAddress_Longitude",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "PickupAddress_PostalCode",
                table: "Shipments");

            migrationBuilder.RenameColumn(
                name: "PickupAddress_Street",
                table: "Shipments",
                newName: "PickupAddress");

            migrationBuilder.RenameColumn(
                name: "PickupAddress_State",
                table: "Shipments",
                newName: "DeliveryAddress");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "StatusHistories",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Shipments",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);
        }
    }
}
