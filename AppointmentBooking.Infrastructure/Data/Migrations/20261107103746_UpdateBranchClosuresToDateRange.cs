using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentBooking.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBranchClosuresToDateRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BranchOperatingHours_Branches_BranchId",
                table: "BranchOperatingHours");

            migrationBuilder.DropIndex(
                name: "IX_BranchClosures_BranchId_ClosureDate",
                table: "BranchClosures");

            migrationBuilder.RenameColumn(
                name: "ClosureDate",
                table: "BranchClosures",
                newName: "StartDate");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "BranchClosures",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "BranchClosures",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateIndex(
                name: "IX_BranchClosures_BranchId_StartDate_EndDate",
                table: "BranchClosures",
                columns: new[] { "BranchId", "StartDate", "EndDate" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_BranchClosures_DateRange",
                table: "BranchClosures",
                sql: "[EndDate] >= [StartDate]");

            migrationBuilder.AddForeignKey(
                name: "FK_BranchOperatingHours_Branches_BranchId",
                table: "BranchOperatingHours",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "BranchId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BranchOperatingHours_Branches_BranchId",
                table: "BranchOperatingHours");

            migrationBuilder.DropIndex(
                name: "IX_BranchClosures_BranchId_StartDate_EndDate",
                table: "BranchClosures");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BranchClosures_DateRange",
                table: "BranchClosures");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "BranchClosures");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "BranchClosures",
                newName: "ClosureDate");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "BranchClosures",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.CreateIndex(
                name: "IX_BranchClosures_BranchId_ClosureDate",
                table: "BranchClosures",
                columns: new[] { "BranchId", "ClosureDate" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BranchOperatingHours_Branches_BranchId",
                table: "BranchOperatingHours",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "BranchId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
