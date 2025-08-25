using Microsoft.EntityFrameworkCore;
using QOS.Areas.Function.Models;
using QOS.Models;

namespace QOS.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserPermission>()
                .HasKey(up => new { up.FactoryID, up.UserName });

            modelBuilder.Entity<Factory_List>().HasKey(fac => new { fac.FactoryID, fac.FactoryName });
        }

        public DbSet<User> Users { get; set; }
        // public DbSet<Menu> Menus { get; set; }
        public DbSet<FaultCode> FaultCodes { get; set; }

        public DbSet<ManageOperation> ManageOperations { get; set; }

        public DbSet<UserPermission> UserPermissions { get; set; }

        public DbSet<Team_List> Team_List { get; set; }

        public DbSet<Factory_List> Factory_List { get; set; }
    }
}
