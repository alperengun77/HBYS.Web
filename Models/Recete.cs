using System.ComponentModel.DataAnnotations;

namespace HBYS.Web.Models
{
    public class Recete
    {
        public int ReceteId { get; set; }

        public int MuayeneId { get; set; }

        public Muayene? Muayene { get; set; }

        [Required]
        [StringLength(100)]
        public string IlacAdi { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Doz { get; set; }

        [StringLength(200)]
        public string? KullanimSekli { get; set; }

        [StringLength(100)]
        public string? KullanimSuresi { get; set; }

        [StringLength(300)]
        public string? Aciklama { get; set; }

        public bool AktifMi { get; set; } = true;

        public DateTime KayitTarihi { get; set; } = DateTime.Now;
    }
}