using Microsoft.EntityFrameworkCore;
using StajYönetimSistemi.Models;

namespace StajYönetimSistemi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Kullanici> Kullanicilar { get; set; }
        public DbSet<Ogrenci> Ogrenciler { get; set; }
        public DbSet<Admin> Adminler { get; set; }
        public DbSet<Evrak> Evraklar { get; set; }
    }
}