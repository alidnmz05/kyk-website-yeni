using Microsoft.EntityFrameworkCore;
using YurtMenu.API.Models;


namespace YurtMenu.API.Data

{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Menu> Menus { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<FoodDictionary> FoodDictionary { get; set; }
        public DbSet<Admin> Admins { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Menu>()
                .HasOne(m => m.City)
                .WithMany(c => c.Menus)
                .HasForeignKey(m => m.CityId)
                .OnDelete(DeleteBehavior.Cascade);


        }

    }
}
