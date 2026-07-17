using System.ComponentModel.DataAnnotations;

namespace HBYS.Web.Models
{
    public class HastaYatis
    {
        public int HastaYatisId { get; set; }

        public int HastaId { get; set; }

        public Hasta? Hasta { get; set; }

        public int DoktorId { get; set; }

        public Doktor? Doktor { get; set; }

        public int YatakId { get; set; }

        public Yatak? Yatak { get; set; }

        [Required(ErrorMessage = "Yatış tarihi boş bırakılamaz.")]
        public DateTime YatisTarihi { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Yatış türü seçilmelidir.")]
        [StringLength(30)]
        public string YatisTuru { get; set; } = "Poliklinik";

        [Required(ErrorMessage = "Yatış nedeni yazılmalıdır.")]
        [StringLength(
            500,
            ErrorMessage = "Yatış nedeni en fazla 500 karakter olabilir."
        )]
        public string YatisNedeni { get; set; } = string.Empty;

        [StringLength(
            500,
            ErrorMessage = "Ön tanı en fazla 500 karakter olabilir."
        )]
        public string? OnTani { get; set; }

        [Required]
        [StringLength(20)]
        public string Durum { get; set; } = "Yatıyor";

        public DateTime? TaburcuTarihi { get; set; }

        [StringLength(
            500,
            ErrorMessage = "Taburcu nedeni en fazla 500 karakter olabilir."
        )]
        public string? TaburcuNedeni { get; set; }

        [StringLength(
            1000,
            ErrorMessage = "Taburcu notu en fazla 1000 karakter olabilir."
        )]
        public string? TaburcuNotu { get; set; }

        public bool AktifMi { get; set; } = true;

        public DateTime KayitTarihi { get; set; } = DateTime.Now;
    }
}