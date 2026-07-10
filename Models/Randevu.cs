using System.ComponentModel.DataAnnotations;

namespace HBYS.Web.Models
{
    public class Randevu
    {
        public int RandevuId { get; set; }

        public int HastaId { get; set; }

        public Hasta? Hasta { get; set; }

        public int DoktorId { get; set; }

        public Doktor? Doktor { get; set; }

        public int PoliklinikId { get; set; }

        public Poliklinik? Poliklinik { get; set; }

        public DateTime RandevuTarihiSaati { get; set; }

        [StringLength(30)]
        public string Durum { get; set; } = "Bekliyor";

        [StringLength(300)]
        public string? Aciklama { get; set; }

        public bool AktifMi { get; set; } = true;

        public DateTime KayitTarihi { get; set; } = DateTime.Now;
    }
}
