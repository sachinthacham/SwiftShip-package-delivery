using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipmentService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeSessionIdToInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StripeSessionId",
                table: "Invoices",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StripeSessionId",
                table: "Invoices");
        }
    }
}
