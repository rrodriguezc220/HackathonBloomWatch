using Microsoft.EntityFrameworkCore;
using HackathonBloomWatch.Models;

namespace HackathonBloomWatch.Data
{
    public class HackathonBWContext : DbContext
    {
        public HackathonBWContext(DbContextOptions<HackathonBWContext> options) : base(options) { }
        public DbSet<EspeciePlanta> EspeciePlanta { get; set; }
        public DbSet<MacizoForestal> MacizoForestal { get; set; }
        public DbSet<Campania> Campania { get; set; }
        public DbSet<CampaniaDetalle> CampaniaDetalle { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EspeciePlanta>().ToTable("EspeciePlanta");
            modelBuilder.Entity<MacizoForestal>().ToTable("MacizoForestal");
            modelBuilder.Entity<Campania>().ToTable("Campania");
            modelBuilder.Entity<CampaniaDetalle>().ToTable("CampaniaDetalle");

            base.OnModelCreating(modelBuilder);
        }
    }
}
