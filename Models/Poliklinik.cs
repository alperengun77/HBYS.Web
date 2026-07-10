using System.ComponentModel.DataAnnotations;

namespace HBYS.Web.Models
{
    public class Poliklinik
    {
        public int PoliklinikId { get; set; }

        [Required]
        [StringLength(100)]
        public string PoliklinikAdi { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Aciklama { get; set; }

        public bool AktifMi { get; set; } = true;

        public List<Doktor> Doktorlar { get; set; } = new List<Doktor>();

        public List<Randevu> Randevular { get; set; } = new List<Randevu>();
    }
}
