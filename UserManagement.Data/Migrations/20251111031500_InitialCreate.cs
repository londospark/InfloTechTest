using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UserManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Forename = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Surname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Forename", "Surname", "Email", "IsActive" },
                values: new object[,]
                {
                    { 1L, "Peter", "Loew", "ploew@example.com", true },
                    { 2L, "Benjamin Franklin", "Gates", "bfgates@example.com", true },
                    { 3L, "Castor", "Troy", "ctroy@example.com", false },
                    { 4L, "Memphis", "Raines", "mraines@example.com", true },
                    { 5L, "Stanley", "Goodspeed", "sgodspeed@example.com", true },
                    { 6L, "H.I.", "McDunnough", "himcdunnough@example.com", true },
                    { 7L, "Cameron", "Poe", "cpoe@example.com", false },
                    { 8L, "Edward", "Malus", "emalus@example.com", false },
                    { 9L, "Damon", "Macready", "dmacready@example.com", false },
                    { 10L, "Johnny", "Blaze", "jblaze@example.com", true },
                    { 11L, "Robin", "Feld", "rfeld@example.com", true }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
