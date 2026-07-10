using System.ComponentModel.DataAnnotations;

namespace HBYS.Web.Models
{
    public class Hasta
    {
        public int HastaId { get; set; }

        [Required(ErrorMessage = "TC kimlik no boş bırakılamaz.")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "TC kimlik no 11 haneli olmalıdır.")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "TC kimlik no sadece rakamlardan oluşmalıdır.")]
        public string TcKimlikNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ad boş bırakılamaz.")]
        [StringLength(50)]
        public string Ad { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad boş bırakılamaz.")]
        [StringLength(50)]
        public string Soyad { get; set; } = string.Empty;

        public DateTime DogumTarihi { get; set; }

        [StringLength(20)]
        public string? Cinsiyet { get; set; }

        [StringLength(11)]
        [RegularExpression(@"^[0-9]*$", ErrorMessage = "Telefon sadece rakamlardan oluşmalıdır.")]
        public string? Telefon { get; set; }

        [StringLength(100)]
        public string? Eposta { get; set; }

        [StringLength(300)]
        public string? Adres { get; set; }

        [StringLength(10)]
        public string? KanGrubu { get; set; }

        public bool AktifMi { get; set; } = true;

        public DateTime KayitTarihi { get; set; } = DateTime.Now;

        public List<Randevu> Randevular { get; set; } = new List<Randevu>();

        public List<Muayene> Muayeneler { get; set; } = new List<Muayene>();
    }
}