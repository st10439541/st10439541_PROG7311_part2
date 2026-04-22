using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace St10439541_PROG7311_P2.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminChangesToServiceRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminComments",
                table: "ServiceRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AdminResponseDate",
                table: "ServiceRequests",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminComments",
                table: "ServiceRequests");

            migrationBuilder.DropColumn(
                name: "AdminResponseDate",
                table: "ServiceRequests");
        }
    }
}
