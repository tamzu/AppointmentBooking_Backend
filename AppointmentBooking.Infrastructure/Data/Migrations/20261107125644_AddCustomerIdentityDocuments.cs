using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentBooking.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerIdentityDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "IdentityDocumentTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<short>(
                name: "EncryptionKeyVersion",
                table: "CustomerIdentityDocuments",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<string>(
                name: "IssuingCountryCode",
                table: "CustomerIdentityDocuments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MaskedDocumentNumber",
                table: "CustomerIdentityDocuments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "IdentityDocumentTypes",
                keyColumn: "IdentityDocumentTypeId",
                keyValue: 1,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "IdentityDocumentTypes",
                keyColumn: "IdentityDocumentTypeId",
                keyValue: 2,
                column: "IsActive",
                value: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "IdentityDocumentTypes");

            migrationBuilder.DropColumn(
                name: "EncryptionKeyVersion",
                table: "CustomerIdentityDocuments");

            migrationBuilder.DropColumn(
                name: "IssuingCountryCode",
                table: "CustomerIdentityDocuments");

            migrationBuilder.DropColumn(
                name: "MaskedDocumentNumber",
                table: "CustomerIdentityDocuments");
        }
    }
}
