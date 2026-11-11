using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentBooking.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentNotificationAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppointmentNotificationAttempt",
                columns: table => new
                {
                    NotificationAttemptId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NotificationId = table.Column<long>(type: "bigint", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ProviderReference = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentNotificationAttempt", x => x.NotificationAttemptId);
                    table.CheckConstraint("CK_NotificationAttempts_AttemptNumber", "[AttemptNumber] > 0");
                    table.CheckConstraint("CK_NotificationAttempts_Status", "[Status] IN ('PROCESSING', 'SENT', 'FAILED')");
                    table.ForeignKey(
                        name: "FK_AppointmentNotificationAttempt_AppointmentNotifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "AppointmentNotifications",
                        principalColumn: "NotificationId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentNotificationAttempt_NotificationId_AttemptNumber",
                table: "AppointmentNotificationAttempt",
                columns: new[] { "NotificationId", "AttemptNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentNotificationAttempt");
        }
    }
}
