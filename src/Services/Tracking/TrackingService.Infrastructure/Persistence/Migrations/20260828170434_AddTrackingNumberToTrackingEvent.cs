using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TrackingService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackingNumberToTrackingEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ShipmentId",
                table: "TrackingEvents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrackingNumber",
                table: "TrackingEvents",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrackingEvents_TrackingNumber",
                table: "TrackingEvents",
                column: "TrackingNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrackingEvents_TrackingNumber",
                table: "TrackingEvents");

            migrationBuilder.DropColumn(
                name: "ShipmentId",
                table: "TrackingEvents");

            migrationBuilder.DropColumn(
                name: "TrackingNumber",
                table: "TrackingEvents");
        }
    }
}
