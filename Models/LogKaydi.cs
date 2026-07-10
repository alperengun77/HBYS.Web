using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations;

namespace HBYS.Web.Models
{
    public class LogKaydi
    {
        public int LogKaydiId { get; set; }

        [Required]
        [StringLength(100)]
        public string KullaniciAdi { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Modul { get; set; } = "Sistem";

        [Required]
        [StringLength(50)]
        public string IslemTipi { get; set; } = "Bilgi";

        [Required]
        [StringLength(100)]
        public string Islem { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Aciklama { get; set; }

        [StringLength(50)]
        public string? IpAdresi { get; set; }

        public DateTime Tarih { get; set; } = DateTime.Now;
    }
}