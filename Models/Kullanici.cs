using System.ComponentModel.DataAnnotations;

namespace HBYS.Web.Models
{
    public class Kullanici
    {
        public int KullaniciId { get; set; }

        [Required]
        [StringLength(50)]
        public string KullaniciAdi { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Sifre { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string AdSoyad { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Eposta { get; set; }

        public int RolId { get; set; }

        public Rol? Rol { get; set; }

        public int? DoktorId { get; set; }

        public Doktor? Doktor { get; set; }

        public bool AktifMi { get; set; } = true;

        public DateTime KayitTarihi { get; set; } = DateTime.Now;
    }
}
