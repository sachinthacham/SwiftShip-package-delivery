using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackingService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConsumerDeduplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessedIntegrationEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EventKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedIntegrationEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedIntegrationEvents_EventType_EventKey",
                table: "ProcessedIntegrationEvents",
                columns: new[] { "EventType", "EventKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessedIntegrationEvents");
        }
    }
}
