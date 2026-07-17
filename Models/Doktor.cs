using System.ComponentModel.DataAnnotations;

namespace HBYS.Web.Models
{
    public class Doktor
    {
        public int DoktorId { get; set; }

        [Required(ErrorMessage = "TC kimlik no boş bırakılamaz.")]
        [StringLength(
            11,
            MinimumLength = 11,
            ErrorMessage = "TC kimlik no 11 haneli olmalıdır."
        )]
        [RegularExpression(
            @"^[0-9]+$",
            ErrorMessage = "TC kimlik no sadece rakamlardan oluşmalıdır."
        )]
        public string TcKimlikNo { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Ad { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Soyad { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Unvan { get; set; }

        [StringLength(11)]
        [RegularExpression(
            @"^[0-9]*$",
            ErrorMessage = "Telefon sadece rakamlardan oluşmalıdır."
        )]
        public string? Telefon { get; set; }

        [StringLength(100)]
        public string? Eposta { get; set; }

        public int PoliklinikId { get; set; }

        public Poliklinik? Poliklinik { get; set; }

        public bool AktifMi { get; set; } = true;

        public DateTime KayitTarihi { get; set; } = DateTime.Now;

        public List<Randevu> Randevular { get; set; } =
            new List<Randevu>();

        public List<Muayene> Muayeneler { get; set; } =
            new List<Muayene>();

        public List<Kullanici> Kullanicilar { get; set; } =
            new List<Kullanici>();

        public List<HastaYatis> HastaYatislari { get; set; } =
            new List<HastaYatis>();
    }
}