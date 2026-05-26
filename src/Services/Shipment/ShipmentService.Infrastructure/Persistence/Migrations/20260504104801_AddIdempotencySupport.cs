using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShipmentService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIdempotencySupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutboundIntegrationEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EventKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboundIntegrationEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentRequestIdempotencies",
                columns: table => new
                {
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentRequestIdempotencies", x => x.IdempotencyKey);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboundIntegrationEvents_EventType_EventKey",
                table: "OutboundIntegrationEvents",
                columns: new[] { "EventType", "EventKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentRequestIdempotencies_ShipmentId",
                table: "ShipmentRequestIdempotencies",
                column: "ShipmentId",
                unique: true,
                filter: "[ShipmentId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutboundIntegrationEvents");

            migrationBuilder.DropTable(
                name: "ShipmentRequestIdempotencies");
        }
    }
}
