using System.ComponentModel.DataAnnotations;

namespace HBYS.Web.Models
{
    public class Oda
    {
        public int OdaId { get; set; }

        public int ServisId { get; set; }

        public Servis? Servis { get; set; }

        [Required(
            ErrorMessage =
                "Oda numarası boş bırakılamaz."
        )]
        [StringLength(20)]
        public string OdaNo { get; set; } =
            string.Empty;

        [Required(
            ErrorMessage =
                "Oda tipi seçilmelidir."
        )]
        [StringLength(30)]
        public string OdaTipi { get; set; } =
            "Standart";

        [Required(
            ErrorMessage =
                "Cinsiyet kısıtlaması seçilmelidir."
        )]
        [StringLength(20)]
        public string CinsiyetKisitlamasi { get; set; } =
            "Karma";

        [StringLength(300)]
        public string? Aciklama { get; set; }

        public bool AktifMi { get; set; } = true;

        public DateTime KayitTarihi { get; set; } =
            DateTime.Now;

        public List<Yatak> Yataklar { get; set; } =
            new List<Yatak>();
    }
}