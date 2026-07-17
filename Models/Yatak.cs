using System.ComponentModel.DataAnnotations;

namespace HBYS.Web.Models
{
    public class Yatak
    {
        public int YatakId { get; set; }

        public int OdaId { get; set; }

        public Oda? Oda { get; set; }

        [Required(ErrorMessage = "Yatak numarası boş bırakılamaz.")]
        [StringLength(20)]
        public string YatakNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Yatak durumu seçilmelidir.")]
        [StringLength(20)]
        public string Durum { get; set; } = "Boş";

        [StringLength(300)]
        public string? Ozellik { get; set; }

        public bool AktifMi { get; set; } = true;

        public DateTime KayitTarihi { get; set; } = DateTime.Now;

        public List<HastaYatis> HastaYatislari { get; set; } =
            new List<HastaYatis>();
    }
}