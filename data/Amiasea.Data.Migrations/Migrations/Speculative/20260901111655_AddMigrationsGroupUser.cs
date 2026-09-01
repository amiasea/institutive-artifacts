using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Amiasea.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMigrationsGroupUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Prefer the group name – it is stable
            const string groupName = "Amiasea-SQL-Migrations";

            migrationBuilder.Sql($@"
            IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'{groupName}')
            BEGIN
                CREATE USER [{groupName}] FROM EXTERNAL PROVIDER;
            END
        ", suppressTransaction: true);

            // Scope permissions better
            migrationBuilder.Sql($@"
            IF IS_ROLEMEMBER('db_owner', '{groupName}') = 0
                ALTER ROLE db_owner ADD MEMBER [{groupName}];
        ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            const string groupName = "Amiasea-SQL-Migrations";
            migrationBuilder.Sql($@"
            IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'{groupName}')
                DROP USER [{groupName}];
        ");
        }
    }
}
