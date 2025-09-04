using Microsoft.EntityFrameworkCore;
using QOS.Areas.Function.Controllers;
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

        public DbSet<Unit_List> Unit_List { get; set; }

        public DbSet<GroupContactList> GroupContactList { get; set; }

        public DbSet<TrackingSetup> TrackingSetup { get; set; }

        public DbSet<TRACKING_InforSetup_Column> TRACKING_InforSetup_Column { get; set; }
        public DbSet<TRACKING_InforSetup_DataType> TRACKING_InforSetup_DataType { get; set; }
        public DbSet<TRACKING_InforSetup_Index> TRACKING_InforSetup_Index { get; set; }
        public DbSet<TRACKING_InforSetup_Name> TRACKING_InforSetup_Name { get; set; }
        public DbSet<TRACKING_InforSetup_Remark> TRACKING_InforSetup_Remark { get; set; }
        public DbSet<TRACKING_InforSetup_Opt> TRACKING_InforSetup_Opt { get; set; }

        public DbSet<TRACKING_ResultSetup_DataType> TRACKING_ResultSetup_DataType { get; set; }
        public DbSet<TRACKING_ResultSetup_Index> TRACKING_ResultSetup_Index { get; set; }
        public DbSet<TRACKING_ResultSetup_Name> TRACKING_ResultSetup_Name { get; set; }
        public DbSet<TRACKING_ResultSetup_Remark> TRACKING_ResultSetup_Remark { get; set; }
        public DbSet<TRACKING_ResultSetup_SelectionData> TRACKING_ResultSetup_SelectionData { get; set; }

        public DbSet<OnlineFile> OnlineFiles { get; set; }
        public DbSet<OnlineFileGroup> OnlineFileGroups { get; set; }
    }
}
