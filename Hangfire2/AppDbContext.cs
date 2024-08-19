using Microsoft.EntityFrameworkCore;

namespace Hangfire2;

public class AppDbContext : DbContext
{
    public DbSet<DataItem> DataItems { get; set; }
    public DbSet<DataItem2> DataItems2 { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options):base(options)
    {
        
    }
}