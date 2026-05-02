using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FCG.API.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarPromocoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Promocoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    JogoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PercentualDesconto = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    Inicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Fim = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promocoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Promocoes_Jogos_JogoId",
                        column: x => x.JogoId,
                        principalTable: "Jogos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Promocoes_JogoId",
                table: "Promocoes",
                column: "JogoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Promocoes");
        }
    }
}
