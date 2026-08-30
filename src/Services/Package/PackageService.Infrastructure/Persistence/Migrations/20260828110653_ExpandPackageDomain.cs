using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PackageService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandPackageDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReceiverAddress",
                table: "Packages",
                newName: "ReceiverAddress_Street");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Packages",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<decimal>(
                name: "DeclaredValue",
                table: "Packages",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryType",
                table: "Packages",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReceiverAddress_City",
                table: "Packages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReceiverAddress_Country",
                table: "Packages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "ReceiverAddress_Latitude",
                table: "Packages",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ReceiverAddress_Longitude",
                table: "Packages",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiverAddress_PostalCode",
                table: "Packages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReceiverAddress_State",
                table: "Packages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeclaredValue",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "DeliveryType",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "ReceiverAddress_City",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "ReceiverAddress_Country",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "ReceiverAddress_Latitude",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "ReceiverAddress_Longitude",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "ReceiverAddress_PostalCode",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "ReceiverAddress_State",
                table: "Packages");

            migrationBuilder.RenameColumn(
                name: "ReceiverAddress_Street",
                table: "Packages",
                newName: "ReceiverAddress");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Packages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);
        }
    }
}
