using System.ComponentModel.DataAnnotations;

namespace HBYS.Web.Models
{
    public class Tahlil
    {
        public int TahlilId { get; set; }

        public int MuayeneId { get; set; }

        public Muayene? Muayene { get; set; }

        [Required]
        [StringLength(100)]
        public string TahlilAdi { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Sonuc { get; set; }

        [StringLength(30)]
        public string Durum { get; set; } = "Istenildi";

        public DateTime IstenmeTarihi { get; set; } = DateTime.Now;

        public DateTime? SonucTarihi { get; set; }

        public bool AktifMi { get; set; } = true;
    }
}