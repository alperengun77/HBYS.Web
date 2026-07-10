using HBYS.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace HBYS.Web.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<Ilac> Ilaclar { get; set; }
        public DbSet<IlacDozu> IlacDozlari { get; set; }
        public DbSet<IlacKullanimSekli> IlacKullanimSekilleri { get; set; }
        public DbSet<IlacKullanimSuresi> IlacKullanimSureleri { get; set; }
        public DbSet<Rol> Roller { get; set; }
        public DbSet<Kullanici> Kullanicilar { get; set; }
        public DbSet<Hasta> Hastalar { get; set; }
        public DbSet<Doktor> Doktorlar { get; set; }
        public DbSet<Poliklinik> Poliklinikler { get; set; }
        public DbSet<Randevu> Randevular { get; set; }
        public DbSet<Muayene> Muayeneler { get; set; }
        public DbSet<Recete> Receteler { get; set; }
        public DbSet<Tahlil> Tahliller { get; set; }
        public DbSet<LogKaydi> LogKayitlari { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Ilac>()
    .HasIndex(i => i.IlacAdi)
    .IsUnique();

            modelBuilder.Entity<IlacDozu>()
                .HasIndex(d => d.DozAdi)
                .IsUnique();

            modelBuilder.Entity<IlacKullanimSekli>()
                .HasIndex(k => k.KullanimSekliAdi)
                .IsUnique();

            modelBuilder.Entity<IlacKullanimSuresi>()
                .HasIndex(s => s.KullanimSuresiAdi)
                .IsUnique();
            modelBuilder.Entity<Hasta>()
                .HasIndex(h => h.TcKimlikNo)
                .IsUnique();

            modelBuilder.Entity<Kullanici>()
                .HasIndex(k => k.KullaniciAdi)
                .IsUnique();

            modelBuilder.Entity<Doktor>()
                .HasIndex(d => d.SicilNo)
                .IsUnique();

            modelBuilder.Entity<Poliklinik>()
                .HasIndex(p => p.PoliklinikAdi)
                .IsUnique();

            modelBuilder.Entity<Kullanici>()
                .HasOne(k => k.Rol)
                .WithMany(r => r.Kullanicilar)
                .HasForeignKey(k => k.RolId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Kullanici>()
                .HasOne(k => k.Doktor)
                .WithMany(d => d.Kullanicilar)
                .HasForeignKey(k => k.DoktorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Doktor>()
                .HasOne(d => d.Poliklinik)
                .WithMany(p => p.Doktorlar)
                .HasForeignKey(d => d.PoliklinikId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Randevu>()
                .HasOne(r => r.Hasta)
                .WithMany(h => h.Randevular)
                .HasForeignKey(r => r.HastaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Randevu>()
                .HasOne(r => r.Doktor)
                .WithMany(d => d.Randevular)
                .HasForeignKey(r => r.DoktorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Randevu>()
                .HasOne(r => r.Poliklinik)
                .WithMany(p => p.Randevular)
                .HasForeignKey(r => r.PoliklinikId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Muayene>()
                .HasOne(m => m.Hasta)
                .WithMany(h => h.Muayeneler)
                .HasForeignKey(m => m.HastaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Muayene>()
                .HasOne(m => m.Doktor)
                .WithMany(d => d.Muayeneler)
                .HasForeignKey(m => m.DoktorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Muayene>()
                .HasOne(m => m.Randevu)
                .WithMany()
                .HasForeignKey(m => m.RandevuId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Recete>()
                .HasOne(r => r.Muayene)
                .WithMany(m => m.Receteler)
                .HasForeignKey(r => r.MuayeneId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Tahlil>()
                .HasOne(t => t.Muayene)
                .WithMany(m => m.Tahliller)
                .HasForeignKey(t => t.MuayeneId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
