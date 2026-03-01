using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankOfGraeme.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBsbAccountNumberUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Bsb_AccountNumber",
                table: "Accounts",
                columns: new[] { "Bsb", "AccountNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Accounts_Bsb_AccountNumber",
                table: "Accounts");
        }
    }
}
