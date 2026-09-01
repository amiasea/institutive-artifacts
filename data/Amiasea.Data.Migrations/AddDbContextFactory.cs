using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class SpeculativeDbContextFactory : IDesignTimeDbContextFactory<SpeculativeDbContext>
{
    public SpeculativeDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Server=localhost;Database=amiasea;Authentication=Active Directory Default;Encrypt=True;";

        var options = new DbContextOptionsBuilder<SpeculativeDbContext>()
            .UseSqlServer(connectionString,
                sql => sql.MigrationsAssembly("Amiasea.Data.Migrations"))
            .Options;

        return new SpeculativeDbContext(options);
    }
}