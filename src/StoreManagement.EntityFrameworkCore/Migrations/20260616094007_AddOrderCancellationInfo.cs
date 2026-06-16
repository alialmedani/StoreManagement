using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StoreManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCancellationInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "AppOrders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancellationTime",
                table: "AppOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreManagement_Orders_CancellationTime",
                table: "AppOrders",
                column: "CancellationTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoreManagement_Orders_CancellationTime",
                table: "AppOrders");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "AppOrders");

            migrationBuilder.DropColumn(
                name: "CancellationTime",
                table: "AppOrders");
        }
    }
}
