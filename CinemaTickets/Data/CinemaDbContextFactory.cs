using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CinemaTickets.Data
{
    public class CinemaDbContextFactory : IDesignTimeDbContextFactory<CinemaDbContext>
    {
        public CinemaDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<CinemaDbContext>();

            optionsBuilder.UseNpgsql(
                "Host=localhost;Port=5432;Database=cinema_tickets;Username=postgres;Password=love"
            );

            return new CinemaDbContext(optionsBuilder.Options);
        }
    }
}