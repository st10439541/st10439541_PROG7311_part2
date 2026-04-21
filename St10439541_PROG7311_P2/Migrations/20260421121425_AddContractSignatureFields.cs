using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace St10439541_PROG7311_P2.Migrations
{
    /// <inheritdoc />
    public partial class AddContractSignatureFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSignedByClient",
                table: "Contracts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SignatureDate",
                table: "Contracts",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSignedByClient",
                table: "Contracts");

            migrationBuilder.DropColumn(
                name: "SignatureDate",
                table: "Contracts");
        }
    }
}
