using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentBooking.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RestrictAppointmentAccessCodeColumnLengths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppointmentAccessCode_Appointments_AppointmentId",
                table: "AppointmentAccessCode");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentAccessCode_AppointmentId",
                table: "AppointmentAccessCode");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "AppointmentAccessCode",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "CodeHash",
                table: "AppointmentAccessCode",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Channel",
                table: "AppointmentAccessCode",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<int>(
                name: "AttemptCount",
                table: "AppointmentAccessCode",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentAccessCode_AppointmentId_ExpiresAt",
                table: "AppointmentAccessCode",
                columns: new[] { "AppointmentId", "ExpiresAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_AppointmentAccessCode_AttemptCount",
                table: "AppointmentAccessCode",
                sql: "[AttemptCount] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AppointmentAccessCode_Channel",
                table: "AppointmentAccessCode",
                sql: "[Channel] IN ('EMAIL', 'SMS')");

            migrationBuilder.AddForeignKey(
                name: "FK_AppointmentAccessCode_Appointments_AppointmentId",
                table: "AppointmentAccessCode",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "AppointmentId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppointmentAccessCode_Appointments_AppointmentId",
                table: "AppointmentAccessCode");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentAccessCode_AppointmentId_ExpiresAt",
                table: "AppointmentAccessCode");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AppointmentAccessCode_AttemptCount",
                table: "AppointmentAccessCode");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AppointmentAccessCode_Channel",
                table: "AppointmentAccessCode");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "AppointmentAccessCode",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<string>(
                name: "CodeHash",
                table: "AppointmentAccessCode",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Channel",
                table: "AppointmentAccessCode",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<int>(
                name: "AttemptCount",
                table: "AppointmentAccessCode",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentAccessCode_AppointmentId",
                table: "AppointmentAccessCode",
                column: "AppointmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppointmentAccessCode_Appointments_AppointmentId",
                table: "AppointmentAccessCode",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "AppointmentId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
