using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentBooking.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerIdentityVerifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerIdentityVerifications_CustomerIdentityDocuments_IdentityDocumentId",
                table: "CustomerIdentityVerifications");

            migrationBuilder.DropIndex(
                name: "IX_CustomerIdentityVerifications_IdentityDocumentId",
                table: "CustomerIdentityVerifications");

            migrationBuilder.DropColumn(
                name: "VerificationReference",
                table: "CustomerIdentityVerifications");

            migrationBuilder.RenameColumn(
                name: "VerificationProvider",
                table: "CustomerIdentityVerifications",
                newName: "ProviderReference");

            migrationBuilder.RenameColumn(
                name: "VerificationMessage",
                table: "CustomerIdentityVerifications",
                newName: "FailureReason");

            migrationBuilder.RenameColumn(
                name: "IdentityDocumentId",
                table: "CustomerIdentityVerifications",
                newName: "CustomerIdentityDocumentId");

            migrationBuilder.RenameColumn(
                name: "IdentityVerificationId",
                table: "CustomerIdentityVerifications",
                newName: "CustomerIdentityVerificationId");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "IdentityVerificationStatuses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "IdentityVerificationStatuses",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "IdentityVerificationStatuses",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "CustomerIdentityVerifications",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<long>(
                name: "CustomerIdentityDocumentIdentityDocumentId",
                table: "CustomerIdentityVerifications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationMethod",
                table: "CustomerIdentityVerifications",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "IdentityVerificationStatuses",
                keyColumn: "IdentityVerificationStatusId",
                keyValue: 1,
                columns: new[] { "Code", "IsActive", "Name" },
                values: new object[] { "PENDING", true, "Pending" });

            migrationBuilder.UpdateData(
                table: "IdentityVerificationStatuses",
                keyColumn: "IdentityVerificationStatusId",
                keyValue: 2,
                columns: new[] { "Code", "IsActive", "Name" },
                values: new object[] { "VERIFIED", true, "Verified" });

            migrationBuilder.UpdateData(
                table: "IdentityVerificationStatuses",
                keyColumn: "IdentityVerificationStatusId",
                keyValue: 3,
                columns: new[] { "Code", "IsActive", "Name" },
                values: new object[] { "REJECTED", true, "Rejected" });

            migrationBuilder.UpdateData(
                table: "IdentityVerificationStatuses",
                keyColumn: "IdentityVerificationStatusId",
                keyValue: 4,
                columns: new[] { "Code", "IsActive", "Name" },
                values: new object[] { "EXPIRED", true, "Expired" });

            migrationBuilder.InsertData(
                table: "IdentityVerificationStatuses",
                columns: new[] { "IdentityVerificationStatusId", "Code", "IsActive", "Name" },
                values: new object[] { 5, "NEEDS_REVIEW", true, "Needs Review" });

            migrationBuilder.CreateIndex(
                name: "IX_IdentityVerificationStatuses_Code",
                table: "IdentityVerificationStatuses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerIdentityVerifications_CustomerIdentityDocumentId_CreatedAt",
                table: "CustomerIdentityVerifications",
                columns: new[] { "CustomerIdentityDocumentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerIdentityVerifications_CustomerIdentityDocumentIdentityDocumentId",
                table: "CustomerIdentityVerifications",
                column: "CustomerIdentityDocumentIdentityDocumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerIdentityVerifications_CustomerIdentityDocuments_CustomerIdentityDocumentId",
                table: "CustomerIdentityVerifications",
                column: "CustomerIdentityDocumentId",
                principalTable: "CustomerIdentityDocuments",
                principalColumn: "IdentityDocumentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerIdentityVerifications_CustomerIdentityDocuments_CustomerIdentityDocumentIdentityDocumentId",
                table: "CustomerIdentityVerifications",
                column: "CustomerIdentityDocumentIdentityDocumentId",
                principalTable: "CustomerIdentityDocuments",
                principalColumn: "IdentityDocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerIdentityVerifications_CustomerIdentityDocuments_CustomerIdentityDocumentId",
                table: "CustomerIdentityVerifications");

            migrationBuilder.DropForeignKey(
                name: "FK_CustomerIdentityVerifications_CustomerIdentityDocuments_CustomerIdentityDocumentIdentityDocumentId",
                table: "CustomerIdentityVerifications");

            migrationBuilder.DropIndex(
                name: "IX_IdentityVerificationStatuses_Code",
                table: "IdentityVerificationStatuses");

            migrationBuilder.DropIndex(
                name: "IX_CustomerIdentityVerifications_CustomerIdentityDocumentId_CreatedAt",
                table: "CustomerIdentityVerifications");

            migrationBuilder.DropIndex(
                name: "IX_CustomerIdentityVerifications_CustomerIdentityDocumentIdentityDocumentId",
                table: "CustomerIdentityVerifications");

            migrationBuilder.DeleteData(
                table: "IdentityVerificationStatuses",
                keyColumn: "IdentityVerificationStatusId",
                keyValue: 5);

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "IdentityVerificationStatuses");

            migrationBuilder.DropColumn(
                name: "CustomerIdentityDocumentIdentityDocumentId",
                table: "CustomerIdentityVerifications");

            migrationBuilder.DropColumn(
                name: "VerificationMethod",
                table: "CustomerIdentityVerifications");

            migrationBuilder.RenameColumn(
                name: "ProviderReference",
                table: "CustomerIdentityVerifications",
                newName: "VerificationProvider");

            migrationBuilder.RenameColumn(
                name: "FailureReason",
                table: "CustomerIdentityVerifications",
                newName: "VerificationMessage");

            migrationBuilder.RenameColumn(
                name: "CustomerIdentityDocumentId",
                table: "CustomerIdentityVerifications",
                newName: "IdentityDocumentId");

            migrationBuilder.RenameColumn(
                name: "CustomerIdentityVerificationId",
                table: "CustomerIdentityVerifications",
                newName: "IdentityVerificationId");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "IdentityVerificationStatuses",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "IdentityVerificationStatuses",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "CustomerIdentityVerifications",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "VerificationReference",
                table: "CustomerIdentityVerifications",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "IdentityVerificationStatuses",
                keyColumn: "IdentityVerificationStatusId",
                keyValue: 1,
                columns: new[] { "Code", "Name" },
                values: new object[] { "NOT_VERIFIED", "Not Verified" });

            migrationBuilder.UpdateData(
                table: "IdentityVerificationStatuses",
                keyColumn: "IdentityVerificationStatusId",
                keyValue: 2,
                columns: new[] { "Code", "Name" },
                values: new object[] { "PENDING", "Pending" });

            migrationBuilder.UpdateData(
                table: "IdentityVerificationStatuses",
                keyColumn: "IdentityVerificationStatusId",
                keyValue: 3,
                columns: new[] { "Code", "Name" },
                values: new object[] { "VERIFIED", "Verified" });

            migrationBuilder.UpdateData(
                table: "IdentityVerificationStatuses",
                keyColumn: "IdentityVerificationStatusId",
                keyValue: 4,
                columns: new[] { "Code", "Name" },
                values: new object[] { "FAILED", "Failed" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerIdentityVerifications_IdentityDocumentId",
                table: "CustomerIdentityVerifications",
                column: "IdentityDocumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerIdentityVerifications_CustomerIdentityDocuments_IdentityDocumentId",
                table: "CustomerIdentityVerifications",
                column: "IdentityDocumentId",
                principalTable: "CustomerIdentityDocuments",
                principalColumn: "IdentityDocumentId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
