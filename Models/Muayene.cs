using System.ComponentModel.DataAnnotations;

namespace HBYS.Web.Models
{
    public class Muayene
    {
        public int MuayeneId { get; set; }

        public int HastaId { get; set; }

        public Hasta? Hasta { get; set; }

        public int DoktorId { get; set; }

        public Doktor? Doktor { get; set; }

        public int? RandevuId { get; set; }

        public Randevu? Randevu { get; set; }

        [StringLength(500)]
        public string? Sikayet { get; set; }

        [StringLength(500)]
        public string? Tani { get; set; }

        [StringLength(1000)]
        public string? TedaviNotu { get; set; }

        public DateTime MuayeneTarihi { get; set; } = DateTime.Now;

        public bool AktifMi { get; set; } = true;

        public List<Recete> Receteler { get; set; } = new List<Recete>();

        public List<Tahlil> Tahliller { get; set; } = new List<Tahlil>();
    }
}