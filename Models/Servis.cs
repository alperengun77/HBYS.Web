using System.ComponentModel.DataAnnotations;

namespace HBYS.Web.Models
{
    public class Servis
    {
        public int ServisId { get; set; }

        [Required(
            ErrorMessage =
                "Servis adı boş bırakılamaz."
        )]
        [StringLength(100)]
        public string ServisAdi { get; set; } =
            string.Empty;

        [StringLength(50)]
        public string? Kat { get; set; }

        [StringLength(500)]
        public string? Aciklama { get; set; }

        public bool AktifMi { get; set; } = true;

        public DateTime KayitTarihi { get; set; } =
            DateTime.Now;

        public List<Oda> Odalar { get; set; } =
            new List<Oda>();
    }
}