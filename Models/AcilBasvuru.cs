using System.ComponentModel.DataAnnotations;

namespace HBYS.Web.Models
{
    public class AcilBasvuru
    {
        public int AcilBasvuruId { get; set; }

        public int HastaId { get; set; }

        public Hasta? Hasta { get; set; }

        public int? DoktorId { get; set; }

        public Doktor? Doktor { get; set; }

        [Required(
            ErrorMessage =
                "Başvuru tarihi boş bırakılamaz."
        )]
        public DateTime BasvuruTarihi { get; set; } =
            DateTime.Now;

        [Required(
            ErrorMessage =
                "Geliş şekli seçilmelidir."
        )]
        [StringLength(30)]
        public string GelisSekli { get; set; } =
            "Ayaktan";

        [Required(
            ErrorMessage =
                "Başvuru şikâyeti yazılmalıdır."
        )]
        [StringLength(
            500,
            ErrorMessage =
                "Şikâyet en fazla 500 karakter olabilir."
        )]
        public string Sikayet { get; set; } =
            string.Empty;

        [Range(
            typeof(decimal),
            "30",
            "45",
            ErrorMessage =
                "Ateş 30 ile 45 derece arasında olmalıdır."
        )]
        public decimal? Ates { get; set; }

        [Range(
            20,
            250,
            ErrorMessage =
                "Nabız 20 ile 250 arasında olmalıdır."
        )]
        public int? Nabiz { get; set; }

        [Range(
            50,
            250,
            ErrorMessage =
                "Sistolik tansiyon 50 ile 250 arasında olmalıdır."
        )]
        public int? TansiyonSistolik { get; set; }

        [Range(
            30,
            180,
            ErrorMessage =
                "Diyastolik tansiyon 30 ile 180 arasında olmalıdır."
        )]
        public int? TansiyonDiastolik { get; set; }

        [Range(
            50,
            100,
            ErrorMessage =
                "Oksijen satürasyonu 50 ile 100 arasında olmalıdır."
        )]
        public int? OksijenSaturasyonu { get; set; }

        [Range(
            5,
            80,
            ErrorMessage =
                "Solunum sayısı 5 ile 80 arasında olmalıdır."
        )]
        public int? SolunumSayisi { get; set; }

        [StringLength(50)]
        public string? BilincDurumu { get; set; }

        [Required(
            ErrorMessage =
                "Triyaj seviyesi seçilmelidir."
        )]
        [StringLength(20)]
        public string TriyajSeviyesi { get; set; } =
            "Yeşil";

        [Required(
            ErrorMessage =
                "Başvuru durumu seçilmelidir."
        )]
        [StringLength(30)]
        public string Durum { get; set; } =
            "Bekliyor";

        [StringLength(
            1000,
            ErrorMessage =
                "Müdahale notu en fazla 1000 karakter olabilir."
        )]
        public string? MudahaleNotu { get; set; }

        [StringLength(
            500,
            ErrorMessage =
                "Sonuç en fazla 500 karakter olabilir."
        )]
        public string? Sonuc { get; set; }

        public bool AktifMi { get; set; } = true;

        public DateTime KayitTarihi { get; set; } =
            DateTime.Now;
    }
}
