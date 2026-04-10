using IcuNotes.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IcuNotes.Migrations
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            // Simple SQLite connection just for creating/updating migrations
            optionsBuilder.UseSqlite("Data Source=IcuNotes.db");

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
