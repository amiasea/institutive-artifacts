using Microsoft.EntityFrameworkCore;

public class SpeculativeDbContext : DbContext
{
    public SpeculativeDbContext(
        DbContextOptions<SpeculativeDbContext> options)
        : base(options)
    {
    }
}