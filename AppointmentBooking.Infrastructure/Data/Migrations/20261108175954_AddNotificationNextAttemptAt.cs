using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentBooking.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationNextAttemptAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NextAttemptAt",
                table: "AppointmentNotifications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentNotifications_Status_NextAttemptAt_CreatedAt",
                table: "AppointmentNotifications",
                columns: new[] { "Status", "NextAttemptAt", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppointmentNotifications_Status_NextAttemptAt_CreatedAt",
                table: "AppointmentNotifications");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                table: "AppointmentNotifications");
        }
    }
}
