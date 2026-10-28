using MarketLine.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketLine.Migrations
{
    [DbContext(typeof(MarketLineDbContext))]
    [Migration("20261028120000_AddItemVariants")]
    public partial class AddItemVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemId = table.Column<int>(type: "int", nullable: false),
                    Size = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    StockQuantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemVariants_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddColumn<int>(
                name: "VariantId",
                table: "OrderLines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantLabel",
                table: "OrderLines",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemVariants_ItemId",
                table: "ItemVariants",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderLines_VariantId",
                table: "OrderLines",
                column: "VariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderLines_ItemVariants_VariantId",
                table: "OrderLines",
                column: "VariantId",
                principalTable: "ItemVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderLines_ItemVariants_VariantId",
                table: "OrderLines");

            migrationBuilder.DropTable(name: "ItemVariants");
            migrationBuilder.DropIndex(name: "IX_OrderLines_VariantId", table: "OrderLines");
            migrationBuilder.DropColumn(name: "VariantId", table: "OrderLines");
            migrationBuilder.DropColumn(name: "VariantLabel", table: "OrderLines");
        }
    }
}
