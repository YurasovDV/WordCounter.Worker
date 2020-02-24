using Microsoft.EntityFrameworkCore;
using WordCounter.Common;

namespace WordCounter.Worker.DAL
{
    public class CountResultsContext : DbContext
    {
        public CountResultsContext(IEnvironmentFacade environmentFacade)
        {
            EnvironmentFacade = environmentFacade;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(ConnectionFactory.GetConnectionString(EnvironmentFacade.BuildDbSettings()));
            base.OnConfiguring(optionsBuilder);
        }

        public DbSet<CountResultRow> CountResults { get; set; }

        public IEnvironmentFacade EnvironmentFacade { get; }
    }
}
