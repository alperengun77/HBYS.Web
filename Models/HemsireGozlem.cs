using System.ComponentModel.DataAnnotations;

namespace HBYS.Web.Models
{
    public class HemsireGozlem
    {
        public int HemsireGozlemId { get; set; }

        public int HastaYatisId { get; set; }

        public HastaYatis? HastaYatis { get; set; }

        public int KaydedenKullaniciId { get; set; }

        public Kullanici? KaydedenKullanici { get; set; }

        [Required(
            ErrorMessage =
                "Gözlem tarihi boş bırakılamaz."
        )]
        public DateTime GozlemTarihi { get; set; } =
            DateTime.Now;

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
                "Büyük tansiyon 50 ile 250 arasında olmalıdır."
        )]
        public int? TansiyonSistolik { get; set; }

        [Range(
            30,
            180,
            ErrorMessage =
                "Küçük tansiyon 30 ile 180 arasında olmalıdır."
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

        [Range(
            20,
            600,
            ErrorMessage =
                "Kan şekeri 20 ile 600 arasında olmalıdır."
        )]
        public int? KanSekeri { get; set; }

        [Range(
            0,
            10,
            ErrorMessage =
                "Ağrı düzeyi 0 ile 10 arasında olmalıdır."
        )]
        public int? AgriDuzeyi { get; set; }

        [Range(
            0,
            10000,
            ErrorMessage =
                "İdrar miktarı 0 ile 10000 ml arasında olmalıdır."
        )]
        public int? IdrarMiktari { get; set; }

        [StringLength(50)]
        public string? BilincDurumu { get; set; }

        [StringLength(100)]
        public string? BeslenmeDurumu { get; set; }

        [StringLength(
            500,
            ErrorMessage =
                "Verilen ilaçlar en fazla 500 karakter olabilir."
        )]
        public string? VerilenIlaclar { get; set; }

        [StringLength(
            500,
            ErrorMessage =
                "Serum takibi en fazla 500 karakter olabilir."
        )]
        public string? SerumTakibi { get; set; }

        [StringLength(
            1000,
            ErrorMessage =
                "Bakım notu en fazla 1000 karakter olabilir."
        )]
        public string? BakimNotu { get; set; }

        public bool KritikDurumVarMi { get; set; }

        [StringLength(
            500,
            ErrorMessage =
                "Kritik durum açıklaması en fazla 500 karakter olabilir."
        )]
        public string? KritikDurumNotu { get; set; }

        public bool AktifMi { get; set; } = true;

        public DateTime KayitTarihi { get; set; } =
            DateTime.Now;
    }
}
