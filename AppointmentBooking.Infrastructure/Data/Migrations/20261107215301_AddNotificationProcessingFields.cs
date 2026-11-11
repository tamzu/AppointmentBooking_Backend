using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentBooking.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationProcessingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "AppointmentNotifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAttemptAt",
                table: "AppointmentNotifications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "AppointmentNotifications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessingStartedAt",
                table: "AppointmentNotifications",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "AppointmentNotifications");

            migrationBuilder.DropColumn(
                name: "LastAttemptAt",
                table: "AppointmentNotifications");

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "AppointmentNotifications");

            migrationBuilder.DropColumn(
                name: "ProcessingStartedAt",
                table: "AppointmentNotifications");
        }
    }
}
